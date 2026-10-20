using System;
using System.IO;
using Abertay.Analytics;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Abertay.Analytics.Editor
{
    /// <summary>One-click export to a new, dated CSV that Excel cannot lock in advance.</summary>
    public static class AnalyticsCsvExportMenu
    {
        [MenuItem("Tools/Abertay Analytics/Export Latest CSV", false, 100)]
        public static void ExportLatest()
        {
            try
            {
                if (EditorApplication.isPlaying && !AnalyticsManager.FlushLocalDataForExport())
                {
                    EditorUtility.DisplayDialog("Analytics CSV export",
                        "The live JSONL log could not be saved. Check the Console; no CSV was exported.", "OK");
                    return;
                }

                string source = EditorApplication.isPlaying
                    ? AnalyticsManager.GetActiveLocalJsonPath()
                    : null;
                if (string.IsNullOrEmpty(source))
                    source = FindMostRecentJsonLog();

                if (string.IsNullOrEmpty(source) || !File.Exists(source))
                {
                    EditorUtility.DisplayDialog("Analytics CSV export",
                        "No analytics log (.jsonl or .json) was found in Assets/Analytics/Events. " +
                        "Record an event first, or use Tools > Abertay Analytics > Export CSV From JSON / JSONL...", "OK");
                    return;
                }

                Export(source);
            }
            catch (Exception error)
            {
                ReportFailure(error);
            }
        }

        [MenuItem("Tools/Abertay Analytics/Export CSV From JSON / JSONL...", false, 101)]
        public static void ExportFromJson()
        {
            string eventsDirectory = GetEventsDirectory();
            string source = EditorUtility.OpenFilePanel("Select analytics JSON or JSONL log", eventsDirectory, "json,jsonl");
            if (string.IsNullOrEmpty(source))
                return;

            try
            {
                // This file may be the active Play-mode log.
                string activePath = AnalyticsManager.GetActiveLocalJsonPath();
                if (EditorApplication.isPlaying && !string.IsNullOrEmpty(activePath)
                    && string.Equals(Path.GetFullPath(source), Path.GetFullPath(activePath),
                        StringComparison.OrdinalIgnoreCase)
                    && !AnalyticsManager.FlushLocalDataForExport())
                {
                    EditorUtility.DisplayDialog("Analytics CSV export",
                        "The active JSONL log could not be saved. Check the Console.", "OK");
                    return;
                }

                Export(source);
            }
            catch (Exception error)
            {
                ReportFailure(error);
            }
        }

        private static string GetEventsDirectory()
        {
            return Path.Combine(Application.dataPath, "Analytics", "Events");
        }

        private static string FindMostRecentJsonLog()
        {
            string directory = GetEventsDirectory();
            if (!Directory.Exists(directory))
                return null;

            string[] jsonlFiles = Directory.GetFiles(directory, "Events*_JSON.jsonl", SearchOption.TopDirectoryOnly);
            string[] jsonFiles = Directory.GetFiles(directory, "Events*_JSON.json", SearchOption.TopDirectoryOnly);
            string latest = null;
            DateTime lastWrite = DateTime.MinValue;
            foreach (string file in jsonlFiles)
            {
                DateTime writeTime = File.GetLastWriteTimeUtc(file);
                if (latest == null || writeTime > lastWrite)
                {
                    latest = file;
                    lastWrite = writeTime;
                }
            }
            // Legacy arrays remain available for export.
            foreach (string file in jsonFiles)
            {
                // Once an environment has migrated, .jsonl is authoritative,
                // even if somebody has subsequently touched the old .json file.
                if (File.Exists(Path.ChangeExtension(file, ".jsonl")))
                    continue;
                DateTime writeTime = File.GetLastWriteTimeUtc(file);
                if (latest == null || writeTime > lastWrite)
                {
                    latest = file;
                    lastWrite = writeTime;
                }
            }
            return latest;
        }

        private static void Export(string source)
        {
            string sourceDirectory = Path.GetDirectoryName(source);
            // Normal analytics logs live inside an Events folder. For an
            // arbitrary externally selected file, keep its exports beside it.
            string exportDirectory = string.Equals(Path.GetFileName(sourceDirectory), "Events",
                    StringComparison.OrdinalIgnoreCase)
                ? Path.Combine(Path.GetDirectoryName(sourceDirectory), "Exports")
                : Path.Combine(sourceDirectory, "Exports");
            Directory.CreateDirectory(exportDirectory);

            string baseName = Path.GetFileNameWithoutExtension(source);
            if (baseName.EndsWith("_JSON", StringComparison.OrdinalIgnoreCase))
                baseName = baseName.Substring(0, baseName.Length - "_JSON".Length);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string target = Path.Combine(exportDirectory, baseName + "_" + timestamp + ".csv");
            int number = 1;
            while (File.Exists(target))
                target = Path.Combine(exportDirectory, baseName + "_" + timestamp + "_" + number++ + ".csv");

            int count = AbertayAnalytics.ExportCsvFromJson(source, target);
            Debug.Log("Abertay Analytics: exported " + count + " events to " + target);

            // Exporting files under Assets requires a refresh for them to appear in Project.
            if (target.StartsWith(Application.dataPath + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
                AssetDatabase.Refresh();

            if (EditorUtility.DisplayDialog("Analytics CSV exported",
                "Exported " + count + " events to:\n" + target +
                "\n\nThe original analytics log is unchanged, and older CSVs remain safe to open in Excel.",
                "Show file", "Close"))
                EditorUtility.RevealInFinder(target);
        }

        private static void ReportFailure(Exception error)
        {
            Debug.LogError("Abertay Analytics: CSV export failed: " + error);
            EditorUtility.DisplayDialog("Analytics CSV export failed",
                "Could not create the CSV. Check the Console for details.\n\n" + error.Message,
                "OK");
        }
    }

    [EditorToolbarElement("AbertayAnalytics/ExportCSV", typeof(SceneView))]
    public class AnalyticsCsvSceneToolbarButton : EditorToolbarButton
    {
        public const string Id = "AbertayAnalytics/ExportCSV";

        public AnalyticsCsvSceneToolbarButton()
        {
            text = "Export CSV";
            tooltip = "Export the current or most recently saved Abertay analytics JSON/JSONL log to a new CSV";
            clicked += AnalyticsCsvExportMenu.ExportLatest;
        }
    }

    [Overlay(typeof(SceneView), "Abertay Analytics", defaultDisplay = true)]
    public class AnalyticsCsvSceneToolbar : ToolbarOverlay
    {
        public AnalyticsCsvSceneToolbar() : base(AnalyticsCsvSceneToolbarButton.Id) { }
    }

#if UNITY_6000_3_OR_NEWER
    // Unity 6.3+ supports extending the main toolbar via a public API.
    // Earlier Unity 6 releases keep the Scene view overlay and Tools menu.
    public static class AnalyticsCsvMainToolbar
    {
        [MainToolbarElement("Abertay Analytics/Export CSV", defaultDockPosition = MainToolbarDockPosition.Right)]
        public static MainToolbarElement CreateExportButton()
        {
            return new MainToolbarButton(
                new MainToolbarContent("Export CSV", "Export the latest Abertay Analytics data to CSV"),
                AnalyticsCsvExportMenu.ExportLatest);
        }
    }
#endif
}
