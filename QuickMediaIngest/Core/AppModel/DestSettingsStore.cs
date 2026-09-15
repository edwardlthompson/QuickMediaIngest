#nullable enable
using System;
using System.IO;
using System.Text.Json.Nodes;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Persists ingest destination and file naming template under AppData.</summary>
    public sealed class DestSettingsStore
    {
        public const string DefaultNaming = "[Date]_[Time]_[Original]";

        private readonly string _path;

        public DestSettingsStore(IAppPaths paths)
        {
            _path = Path.Combine(paths.AppDataRoot, "dest.json");
        }

        public static string DefaultDestination()
        {
            string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            if (string.IsNullOrWhiteSpace(pictures))
            {
                pictures = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Pictures");
            }

            return Path.Combine(pictures, "QuickMediaIngest");
        }

        public void ApplyTo(IngestBenchAppModel model)
        {
            DestFile file = Load();
            model.DestinationRoot = string.IsNullOrWhiteSpace(file.DestinationRoot)
                ? DefaultDestination()
                : file.DestinationRoot;
            model.NamingTemplate = string.IsNullOrWhiteSpace(file.NamingTemplate)
                ? DefaultNaming
                : file.NamingTemplate;
            model.DestFolderTemplate = file.DestFolderTemplate ?? string.Empty;
            model.Naming.Preset = string.IsNullOrWhiteSpace(file.NamingPreset)
                ? FileNamingBuilder.PresetCustom
                : file.NamingPreset;
            model.Naming.Lowercase = file.NamingLowercase;
            model.Naming.ShootNameSample = string.IsNullOrWhiteSpace(file.NamingShootNameSample)
                ? "my-shoot"
                : file.NamingShootNameSample;
            IngestBenchNaming.Hydrate(model);
        }

        public string PeekRoot() => Load().DestinationRoot;

        public void Save(IngestBenchAppModel model)
        {
            try
            {
                string? dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var obj = new JsonObject
                {
                    ["DestinationRoot"] = model.DestinationRoot ?? string.Empty,
                    ["NamingTemplate"] = string.IsNullOrWhiteSpace(model.NamingTemplate)
                        ? DefaultNaming
                        : model.NamingTemplate,
                    ["DestFolderTemplate"] = model.DestFolderTemplate ?? string.Empty,
                    ["NamingPreset"] = model.Naming.Preset,
                    ["NamingLowercase"] = model.Naming.Lowercase,
                    ["NamingShootNameSample"] = model.Naming.ShootNameSample,
                };
                File.WriteAllText(_path, obj.ToJsonString());
            }
            catch
            {
                // Best effort.
            }
        }

        private DestFile Load()
        {
            try
            {
                if (!File.Exists(_path))
                {
                    return new DestFile();
                }

                JsonObject obj = JsonNode.Parse(File.ReadAllText(_path)) as JsonObject ?? new JsonObject();
                return new DestFile
                {
                    DestinationRoot = Str(obj, "DestinationRoot"),
                    NamingTemplate = Str(obj, "NamingTemplate"),
                    DestFolderTemplate = Str(obj, "DestFolderTemplate"),
                    NamingPreset = Str(obj, "NamingPreset"),
                    NamingLowercase = Flag(obj, "NamingLowercase", true),
                    NamingShootNameSample = Str(obj, "NamingShootNameSample"),
                };
            }
            catch
            {
                return new DestFile();
            }
        }

        private static string Str(JsonObject obj, string name)
        {
            JsonNode? node = obj[name];
            if (node is JsonValue value && value.TryGetValue(out string? text) && !string.IsNullOrEmpty(text))
            {
                return text;
            }

            string? raw = node?.ToString();
            return string.IsNullOrWhiteSpace(raw) ? string.Empty : raw.Trim('"');
        }

        private static bool Flag(JsonObject obj, string name, bool fallback) =>
            obj[name] is JsonValue value && value.TryGetValue(out bool flag) ? flag : fallback;

        private sealed class DestFile
        {
            public string DestinationRoot { get; set; } = string.Empty;

            public string NamingTemplate { get; set; } = string.Empty;

            public string DestFolderTemplate { get; set; } = string.Empty;

            public string NamingPreset { get; set; } = string.Empty;

            public bool NamingLowercase { get; set; } = true;

            public string NamingShootNameSample { get; set; } = "my-shoot";
        }
    }
}
