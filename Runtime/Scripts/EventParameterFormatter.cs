using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Abertay.Analytics
{
    internal static class EventParameterFormatter
    {
        public static Dictionary<string, object> Normalize(Dictionary<string, object> parameters)
        {
            var result = new Dictionary<string, object>();
            if (parameters == null)
                return result;

            var active = new HashSet<object>();
            foreach (KeyValuePair<string, object> entry in parameters)
            {
                if (string.IsNullOrWhiteSpace(entry.Key))
                    throw new ArgumentException("Analytics parameter names cannot be empty.");
                result.Add(entry.Key, NormalizeValue(entry.Value, 0, active));
            }
            return result;
        }

        private static JToken NormalizeValue(object value, int depth, HashSet<object> active)
        {
            if (depth > 12)
                throw new ArgumentException("Analytics parameter nesting exceeds 12 levels.");
            if (value == null)
                return JValue.CreateNull();
            if (value is JToken token)
                return token.DeepClone();
            if (value is float single)
                return new JValue(Finite(single));
            if (value is double number)
                return new JValue(Finite(number));
            if (value is string || value is bool || value is char || value is decimal ||
                value is byte || value is sbyte || value is short || value is ushort ||
                value is int || value is uint || value is long || value is ulong)
                return new JValue(value);
            if (value is Enum)
                return new JValue(value.ToString());
            if (value is Guid guid)
                return new JValue(guid.ToString("D"));
            if (value is DateTime date)
                return new JValue(date.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
            if (value is DateTimeOffset offset)
                return new JValue(offset.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
            if (value is Vector2 v2)
                return new JObject(new JProperty("x", Finite(v2.x)), new JProperty("y", Finite(v2.y)));
            if (value is Vector3 v3)
                return new JObject(new JProperty("x", Finite(v3.x)), new JProperty("y", Finite(v3.y)),
                    new JProperty("z", Finite(v3.z)));
            if (value is Vector4 v4)
                return new JObject(new JProperty("x", Finite(v4.x)), new JProperty("y", Finite(v4.y)),
                    new JProperty("z", Finite(v4.z)), new JProperty("w", Finite(v4.w)));
            if (value is Vector2Int v2i)
                return new JObject(new JProperty("x", v2i.x), new JProperty("y", v2i.y));
            if (value is Vector3Int v3i)
                return new JObject(new JProperty("x", v3i.x), new JProperty("y", v3i.y),
                    new JProperty("z", v3i.z));
            if (value is Quaternion rotation)
                return new JObject(new JProperty("x", Finite(rotation.x)), new JProperty("y", Finite(rotation.y)),
                    new JProperty("z", Finite(rotation.z)), new JProperty("w", Finite(rotation.w)));
            if (value is Color color)
                return new JObject(new JProperty("r", Finite(color.r)), new JProperty("g", Finite(color.g)),
                    new JProperty("b", Finite(color.b)), new JProperty("a", Finite(color.a)));
            if (value is Color32 color32)
                return new JObject(new JProperty("r", color32.r), new JProperty("g", color32.g),
                    new JProperty("b", color32.b), new JProperty("a", color32.a));
            if (value is UnityEngine.Object)
                throw new ArgumentException("Analytics cannot record Unity objects directly. Record a name, ID or position instead.");

            if (value is IDictionary dictionary)
            {
                if (!active.Add(value))
                    throw new ArgumentException("Analytics parameter dictionaries must not contain reference cycles.");
                try
                {
                    var result = new JObject();
                    foreach (DictionaryEntry pair in dictionary)
                    {
                        if (!(pair.Key is string key))
                            throw new ArgumentException("Analytics nested dictionaries require string keys.");
                        result.Add(key, NormalizeValue(pair.Value, depth + 1, active));
                    }
                    return result;
                }
                finally { active.Remove(value); }
            }
            if (value is IList list)
            {
                if (!active.Add(value))
                    throw new ArgumentException("Analytics parameter lists must not contain reference cycles.");
                try
                {
                    var result = new JArray();
                    foreach (object item in list)
                        result.Add(NormalizeValue(item, depth + 1, active));
                    return result;
                }
                finally { active.Remove(value); }
            }
            throw new ArgumentException("Unsupported analytics parameter type: " + value.GetType().FullName);
        }

        private static float Finite(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentException("Analytics numbers must be finite.");
            return value;
        }

        private static double Finite(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentException("Analytics numbers must be finite.");
            return value;
        }

        public static Dictionary<string, string> FlattenForCsv(Dictionary<string, object> parameters)
        {
            var fields = new Dictionary<string, string>();
            if (parameters == null)
                return fields;

            foreach (KeyValuePair<string, object> pair in parameters)
            {
                JToken token = pair.Value as JToken ??
                    (pair.Value == null ? JValue.CreateNull() : JToken.FromObject(pair.Value));
                Flatten(fields, EscapeSegment(pair.Key), token);
            }
            return fields;
        }

        private static void Flatten(Dictionary<string, string> fields, string name, JToken token)
        {
            if (token is JObject obj && obj.HasValues)
            {
                foreach (JProperty property in obj.Properties())
                    Flatten(fields, name + "." + EscapeSegment(property.Name), property.Value);
                return;
            }

            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
            {
                fields[name] = "";
                return;
            }

            if (token is JValue scalar)
            {
                object value = scalar.Value;
                fields[name] = value is IFormattable formattable
                    ? formattable.ToString(null, CultureInfo.InvariantCulture)
                    : value?.ToString() ?? "";
                return;
            }

            fields[name] = token.ToString(Formatting.None);
        }

        private static string EscapeSegment(string segment)
        {
            return segment.Replace("\\", "\\\\").Replace(".", "\\.");
        }
    }
}
