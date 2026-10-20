using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Abertay.Analytics
{
    /// <summary>
    /// Appends one complete JSON object per line. Only the main/calling thread
    /// serializes events; the background worker sees immutable strings only.
    /// </summary>
    public class AbertayAnalytics : IAnalytics
    {
        private const string UserIdPrefsKey = "UserID";
        private const int MaximumBatchSize = 256;
        private static readonly UTF8Encoding LogEncoding = new UTF8Encoding(false, true);
        private static readonly JsonSerializerSettings EventSerializerSettings = new JsonSerializerSettings
        {
            DateParseHandling = DateParseHandling.None,
            Converters = new List<JsonConverter> { new UnityVector3Converter() }
        };

        private readonly object saveLock = new object();
        private Queue<string> pendingLines = new Queue<string>();
        private Task<bool> writerTask = Task.FromResult(true);
        private bool writerRunning;
        private bool shuttingDown;

        private string m_UserID = "";
        private string filename = ""; // Absolute base path, without _JSON.jsonl.
        public string SessionID { get; private set; }

        /// <summary>The active append-only event log (JSON Lines, not a JSON array).</summary>
        public string JsonFilePath => string.IsNullOrEmpty(filename) ? null : filename + "_JSON.jsonl";

        void IAnalytics.Initialise(Action callback, string environmentName)
        {
            EnsureUserID();
            InitialiseStorage(environmentName);
            callback?.Invoke();
        }

        void IAnalytics.InitialiseWithCustomID(string userID, Action callback, string environmentName)
        {
            if (string.IsNullOrWhiteSpace(userID))
            {
                Debug.LogWarning("Custom analytics user ID was empty; using the saved or generated default ID.");
                EnsureUserID();
            }
            else
            {
                SetUserID(userID);
            }

            InitialiseStorage(environmentName);
            callback?.Invoke();
        }

        private void EnsureUserID()
        {
            if (!string.IsNullOrWhiteSpace(m_UserID) && m_UserID != "NULL")
                return;

            string stored = PlayerPrefs.GetString(UserIdPrefsKey, "");
            SetUserID(string.IsNullOrWhiteSpace(stored) || stored == "NULL"
                ? Guid.NewGuid().ToString("N")
                : stored);
        }

        private void InitialiseStorage(string environmentName)
        {
            if (!string.IsNullOrEmpty(filename) && !FlushPendingWrites())
                throw new IOException("Unable to finish the previous analytics recording.");

            string environment = environmentName ?? "";
#if UNITY_EDITOR
            string root = Application.dataPath;
#else
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
#endif
            string directory = Path.Combine(root, "Analytics", "Events");
            Directory.CreateDirectory(directory);
            string newFilename = Path.Combine(directory, "Events" +
                (environment.Length > 0 ? "_" + environment : ""));

            string previousFilename = filename;
            filename = newFilename;
            try
            {
                InitialiseLogFile();
            }
            catch
            {
                filename = previousFilename;
                throw;
            }

            lock (saveLock)
            {
                pendingLines.Clear();
                shuttingDown = false;
                SessionID = Guid.NewGuid().ToString("N");
            }
        }

        // Retained for source compatibility with earlier versions of the package.
        public bool CreateDirectory(string dir)
        {
            Directory.CreateDirectory(dir);
            return true;
        }

        private void InitialiseLogFile()
        {
            string newPath = JsonFilePath;
            if (!File.Exists(newPath))
            {
                string legacyPath = filename + "_JSON.json";
                if (File.Exists(legacyPath))
                {
                    MigrateLegacyJson(legacyPath, newPath);
                }
                else
                {
                    using (File.Create(newPath)) { }
                }
            }

            RepairLastLineIfNecessary(newPath);
        }

        private static void MigrateLegacyJson(string oldPath, string newPath)
        {
            var oldEvents = JsonConvert.DeserializeObject<List<CustomEvent>>(
                File.ReadAllText(oldPath, Encoding.UTF8), EventSerializerSettings);
            if (oldEvents == null)
                throw new InvalidDataException("Legacy analytics JSON does not contain an event array: " + oldPath);

            string stagingPath = newPath + ".migrating";
            try
            {
                using (var stream = new FileStream(stagingPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (var writer = new StreamWriter(stream, LogEncoding, 4096, true))
                    {
                        foreach (CustomEvent ev in oldEvents)
                            writer.WriteLine(JsonConvert.SerializeObject(ev, Formatting.None, EventSerializerSettings));
                        writer.Flush();
                    }
                    stream.Flush(true);
                }

                File.Move(stagingPath, newPath);
                Debug.Log("Abertay Analytics: migrated " + oldEvents.Count +
                    " old events to JSON Lines. Original retained: " + oldPath);
            }
            finally
            {
                if (File.Exists(stagingPath))
                    File.Delete(stagingPath);
            }
        }

        /// <summary>
        /// A process can die halfway through a line. Keep all completed lines;
        /// if the tail parses as a full object, terminate it with a newline.
        /// Otherwise save the fragment separately and truncate only that tail.
        /// Run before appending too, in case a previous I/O error left a tail.
        /// </summary>
        private static void RepairLastLineIfNecessary(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                if (stream.Length == 0)
                    return;

                stream.Position = stream.Length - 1;
                if (stream.ReadByte() == '\n')
                    return;

                long start = 0;
                for (long pos = stream.Length - 1; pos >= 0; --pos)
                {
                    stream.Position = pos;
                    if (stream.ReadByte() == '\n')
                    {
                        start = pos + 1;
                        break;
                    }
                }

                long tailLength = stream.Length - start;
                if (tailLength > int.MaxValue)
                    throw new InvalidDataException("An analytics JSONL line exceeds the supported size.");
                byte[] tail = new byte[(int)tailLength];
                stream.Position = start;
                int read = 0;
                while (read < tail.Length)
                {
                    int count = stream.Read(tail, read, tail.Length - read);
                    if (count == 0)
                        throw new EndOfStreamException("Unable to read the final analytics event.");
                    read += count;
                }

                try
                {
                    string text = LogEncoding.GetString(tail);
                    if (JObject.Parse(text) != null)
                    {
                        stream.Position = stream.Length;
                        stream.WriteByte((byte)'\n');
                        stream.Flush(true);
                        return;
                    }
                }
                catch (Exception error) when (error is JsonException || error is DecoderFallbackException)
                {
                }

                string fragmentPath = path + ".partial_" +
                    DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fffffff", CultureInfo.InvariantCulture) +
                    "_" + Guid.NewGuid().ToString("N") + ".txt";
                File.WriteAllBytes(fragmentPath, tail); // Don't truncate if preserving the fragment fails.
                stream.SetLength(start);
                stream.Flush(true);
                Debug.LogWarning("Abertay Analytics: discarded an incomplete final JSONL line. " +
                    "Its bytes were preserved in " + fragmentPath);
            }
        }

        public void SendCustomEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                Debug.LogError("Abertay Analytics: event name cannot be empty.");
                return;
            }

            if (string.IsNullOrEmpty(filename))
            {
                Debug.LogError("Abertay Analytics must be initialised before recording events.");
                return;
            }

            // Serialize synchronously so the caller can safely reuse/mutate its
            // dictionary and Unity objects never cross to a worker thread.
            string line;
            try
            {
                var ev = new CustomEvent
                {
                    eventTimestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                    userID = m_UserID,
                    sessionID = SessionID,
                    sceneName = SceneManager.GetActiveScene().name,
                    eventName = eventName,
                    eventUUID = Guid.NewGuid().ToString("N"),
                    eventParams = EventParameterFormatter.Normalize(parameters)
                };
                line = JsonConvert.SerializeObject(ev, Formatting.None, EventSerializerSettings);
            }
            catch (Exception error)
            {
                Debug.LogError("Abertay Analytics: event could not be serialized, so it was not recorded: " + error);
                return;
            }

            lock (saveLock)
            {
                if (shuttingDown)
                {
                    Debug.LogWarning("Ignoring analytics event after shutdown has started.");
                    return;
                }

                pendingLines.Enqueue(line);
                if (!writerRunning)
                {
                    writerRunning = true;
                    writerTask = Task.Run(ProcessPendingSaves);
                }
            }
        }

        private bool ProcessPendingSaves()
        {
            while (true)
            {
                var batch = new List<string>(MaximumBatchSize);
                lock (saveLock)
                {
                    while (pendingLines.Count != 0 && batch.Count < MaximumBatchSize)
                        batch.Add(pendingLines.Dequeue());

                    if (batch.Count == 0)
                    {
                        // The sender uses this very same lock, so no event can
                        // be stranded between the empty check and worker exit.
                        writerRunning = false;
                        return true;
                    }
                }

                try
                {
                    AppendBatch(batch);
                }
                catch (Exception error)
                {
                    lock (saveLock)
                    {
                        // Put the unsuccessful batch back at the FRONT, retaining
                        // chronological order and leaving it available for retry.
                        var restored = new Queue<string>(batch);
                        while (pendingLines.Count != 0)
                            restored.Enqueue(pendingLines.Dequeue());
                        pendingLines = restored;
                        writerRunning = false;
                    }
                    Debug.LogError("Abertay Analytics: unable to append events (queued for retry): " + error);
                    return false;
                }
            }
        }

        private void AppendBatch(List<string> batch)
        {
            var payload = new StringBuilder();
            foreach (string line in batch)
                payload.Append(line).Append('\n');
            byte[] bytes = LogEncoding.GetBytes(payload.ToString());

            // One writer owns the log. Roll back to the prior length if any
            // part of the append throws; the queue is then safe to retry.
            string path = JsonFilePath;
            RepairLastLineIfNecessary(path);
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                long checkpoint = stream.Length;
                stream.Position = checkpoint;
                try
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                catch
                {
                    try
                    {
                        stream.SetLength(checkpoint);
                        stream.Flush(true);
                    }
                    catch (Exception rollbackError)
                    {
                        Debug.LogError("Abertay Analytics: append rollback failed; startup recovery " +
                            "will examine the last line. " + rollbackError);
                    }
                    throw;
                }
            }
        }

        /// <summary>
        /// Convert either a legacy JSON array (.json) or JSON Lines (.jsonl)
        /// to a NEW CSV. Preserves the public v0.8.2 method name.
        /// </summary>
        public static int ExportCsvFromJson(string jsonPath, string csvPath)
        {
            if (string.IsNullOrWhiteSpace(jsonPath))
                throw new ArgumentException("JSON path must not be empty.", nameof(jsonPath));
            if (string.IsNullOrWhiteSpace(csvPath))
                throw new ArgumentException("CSV path must not be empty.", nameof(csvPath));

            List<CustomEvent> source = LoadEventsForExport(jsonPath);
            string directory = Path.GetDirectoryName(csvPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using (var stream = new FileStream(csvPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                writer.Write(MakeCsv(source));
            return source.Count;
        }

        private static List<CustomEvent> LoadEventsForExport(string path)
        {
            if (string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
            {
                return JsonConvert.DeserializeObject<List<CustomEvent>>(
                    File.ReadAllText(path, Encoding.UTF8), EventSerializerSettings) ??
                    throw new InvalidDataException("Analytics JSON does not contain an event array.");
            }

            if (!string.Equals(Path.GetExtension(path), ".jsonl", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Select an analytics .json or .jsonl log.");

            var result = new List<CustomEvent>();
            using (var reader = new StreamReader(path, Encoding.UTF8, true))
            {
                string line;
                int lineNumber = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    ++lineNumber;
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    try
                    {
                        CustomEvent ev = JsonConvert.DeserializeObject<CustomEvent>(line, EventSerializerSettings);
                        if (ev == null)
                            throw new InvalidDataException("The event is null.");
                        result.Add(ev);
                    }
                    catch (Exception error) when (error is JsonException || error is InvalidDataException)
                    {
                        throw new InvalidDataException("Invalid analytics JSONL at line " +
                            lineNumber + " of " + path, error);
                    }
                }
            }
            return result;
        }

        /// <summary>Drain the writer, including any queued events. Safe to call during Play mode.</summary>
        public bool FlushPendingWrites()
        {
            while (true)
            {
                Task<bool> activeTask = null;
                bool writeSynchronously = false;
                lock (saveLock)
                {
                    if (!writerRunning && pendingLines.Count == 0)
                        return true;

                    if (writerRunning)
                        activeTask = writerTask;
                    else
                    {
                        writerRunning = true;
                        writeSynchronously = true;
                    }
                }

                try
                {
                    bool succeeded = writeSynchronously
                        ? ProcessPendingSaves()
                        : activeTask.GetAwaiter().GetResult();
                    if (!succeeded)
                        return false; // Never spin forever on an I/O failure.
                }
                catch (Exception error)
                {
                    Debug.LogError("Abertay Analytics: error flushing the JSONL log: " + error);
                    return false;
                }
            }
        }

        private static string MakeCsv(List<CustomEvent> source)
        {
            var keys = new List<string>();
            var seen = new HashSet<string>();
            var rows = new List<Dictionary<string, string>>(source.Count);
            foreach (CustomEvent ev in source)
            {
                Dictionary<string, string> fields = EventParameterFormatter.FlattenForCsv(ev.eventParams);
                rows.Add(fields);
                foreach (string key in fields.Keys)
                    if (seen.Add(key)) keys.Add(key);
            }

            var csv = new StringBuilder();
            csv.Append("Timestamp,UserID,Event Name,UUID,SessionID,Scene");
            foreach (string key in keys)
                csv.Append(',').Append(EscapeCsv(key));
            csv.AppendLine();

            for (int i = 0; i < source.Count; ++i)
            {
                CustomEvent ev = source[i];
                Dictionary<string, string> fields = rows[i];
                csv.Append(EscapeCsv(ev.eventTimestamp)).Append(',')
                   .Append(EscapeCsv(ev.userID)).Append(',')
                   .Append(EscapeCsv(ev.eventName)).Append(',')
                   .Append(EscapeCsv(ev.eventUUID)).Append(',')
                   .Append(EscapeCsv(ev.sessionID)).Append(',')
                   .Append(EscapeCsv(ev.sceneName));

                foreach (string key in keys)
                {
                    csv.Append(',');
                    if (fields.TryGetValue(key, out string value))
                        csv.Append(EscapeCsv(value));
                }
                csv.AppendLine();
            }
            return csv.ToString();
        }

        private static string EscapeCsv(object value)
        {
            if (value == null) return "";
            string field = value is JToken jsonToken
                ? jsonToken.ToString(Formatting.None)
                : value is IFormattable formattable
                    ? formattable.ToString(null, CultureInfo.InvariantCulture)
                    : value.ToString();
            if (field == null) return "";

            return field.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0
                ? "\"" + field.Replace("\"", "\"\"") + "\""
                : field;
        }

        public void SetUserID(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                Debug.LogWarning("Analytics user ID cannot be empty.");
                return;
            }
            m_UserID = userId;
            PlayerPrefs.SetString(UserIdPrefsKey, m_UserID);
            PlayerPrefs.Save();
        }

#if GAMEANALYTICS
        void IAnalytics.SendCustomEvent(string eventName, Dictionary<string, object> parameters, float GA_Value = 0.0f)
        {
            SendCustomEvent(eventName, parameters);
        }
#endif

        public void OnQuit()
        {
            lock (saveLock)
                shuttingDown = true;

            if (!FlushPendingWrites())
                Debug.LogError("Abertay Analytics: some events could not be persisted on shutdown.");

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        private sealed class UnityVector3Converter : JsonConverter<Vector3>
        {
            public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("x");
                writer.WriteValue(value.x);
                writer.WritePropertyName("y");
                writer.WriteValue(value.y);
                writer.WritePropertyName("z");
                writer.WriteValue(value.z);
                writer.WriteEndObject();
            }

            public override Vector3 ReadJson(JsonReader reader, Type objectType,
                Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                JObject obj = JObject.Load(reader);
                return new Vector3(obj.Value<float>("x"), obj.Value<float>("y"), obj.Value<float>("z"));
            }
        }
    }
}
