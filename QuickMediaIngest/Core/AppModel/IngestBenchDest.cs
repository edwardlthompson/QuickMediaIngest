#nullable enable
using System;
using System.IO;
using System.Text.Json.Nodes;
using QuickMediaIngest.Core;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Destination preset folders (Pictures / Documents / Custom).</summary>
    public static class IngestBenchDest
    {
        public static void ApplyPreset(IngestBenchHost host, string? preset)
        {
            if (string.Equals(preset, "Custom", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string root = string.Equals(preset, "Documents", StringComparison.OrdinalIgnoreCase)
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            if (string.IsNullOrWhiteSpace(root))
            {
                root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }

            host.SetDestination(Path.Combine(root, "QuickMediaIngest"));
        }

        public static void RememberBrowse(IngestBenchHost host, string path)
        {
            host.SetDestination(path);
            host.Model.Prefs.DestinationPreset = "Custom";
            WriteCustomPreset(host.Paths);
        }

        public static void KeepCustomWhenDestDiffers(IngestBenchHost host)
        {
            string saved = new DestSettingsStore(host.Paths).PeekRoot();
            if (!string.IsNullOrWhiteSpace(saved))
            {
                host.Model.DestinationRoot = Path.GetFullPath(saved);
            }

            string dest = host.Model.DestinationRoot ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dest))
            {
                return;
            }

            string pictures = Path.GetFullPath(DestSettingsStore.DefaultDestination());
            string current = Path.GetFullPath(dest);
            if (string.Equals(current, pictures, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            host.Model.Prefs.DestinationPreset = "Custom";
            WriteCustomPreset(host.Paths);
        }

        private static void WriteCustomPreset(IAppPaths paths)
        {
            try
            {
                string file = Path.Combine(paths.AppDataRoot, "prefs.json");
                JsonObject obj = File.Exists(file)
                    ? JsonNode.Parse(File.ReadAllText(file)) as JsonObject ?? new JsonObject()
                    : new JsonObject();
                obj["DestinationPreset"] = "Custom";
                File.WriteAllText(file, obj.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }
            catch
            {
                // Best effort.
            }
        }
    }
}
