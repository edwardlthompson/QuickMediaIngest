#nullable enable
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Persists ingest-bench preferences (<c>prefs.json</c>) with trim-safe JSON.</summary>
    public sealed partial class PrefsStore
    {
        private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

        private readonly string _path;

        public PrefsStore(IAppPaths paths) =>
            _path = Path.Combine(paths.AppDataRoot, "prefs.json");

        public FileDto Load()
        {
            try
            {
                return File.Exists(_path) ? Parse(File.ReadAllText(_path)) : new FileDto();
            }
            catch
            {
                return new FileDto();
            }
        }

        public void Save(FileDto dto)
        {
            try
            {
                string? dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(_path, Format(dto));
            }
            catch
            {
                // Best effort.
            }
        }

        public static string Format(FileDto dto)
        {
            var obj = new JsonObject
            {
                ["Theme"] = dto.Theme ?? "Dark",
                ["Language"] = dto.Language ?? "en",
                ["StripGps"] = dto.StripGps,
                ["SecondaryDestination"] = dto.SecondaryDestination ?? string.Empty,
                ["CreatorStamp"] = dto.CreatorStamp ?? string.Empty,
                ["CopyrightStamp"] = dto.CopyrightStamp ?? string.Empty,
                ["WriteChecksumManifest"] = dto.WriteChecksumManifest,
                ["WriteXmpSidecar"] = dto.WriteXmpSidecar,
                ["WatchFolder"] = dto.WatchFolder ?? string.Empty,
                ["TimeZoneMode"] = dto.TimeZoneMode ?? "CameraAsIs",
                ["BatchRenameBase"] = dto.BatchRenameBase ?? "Shoot",
                ["EjectAfterImport"] = dto.EjectAfterImport,
                ["DestinationPreset"] = dto.DestinationPreset ?? "Custom",
                ["DuplicatePolicy"] = dto.DuplicatePolicy ?? "Suffix",
                ["VerificationMode"] = dto.VerificationMode ?? "Fast",
                ["GroupRawJpeg"] = dto.GroupRawJpeg,
                ["ExpandStacked"] = dto.ExpandStacked,
                ["ConfirmBeforeImport"] = dto.ConfirmBeforeImport,
                ["DeleteAfterImport"] = dto.DeleteAfterImport,
                ["ThumbnailSize"] = dto.ThumbnailSize,
                ["TimeBetweenShootsHours"] = dto.TimeBetweenShootsHours,
                ["FilterFileType"] = string.IsNullOrWhiteSpace(dto.FilterFileType) ? "All" : dto.FilterFileType,
            };
            if (dto.PreferAdb.HasValue)
            {
                obj["PreferAdb"] = dto.PreferAdb.Value;
            }

            if (dto.AllGroupsExpanded.HasValue)
            {
                obj["AllGroupsExpanded"] = dto.AllGroupsExpanded.Value;
            }

            PrefsLayout.Write(obj, dto);
            return obj.ToJsonString(Pretty);
        }

        public static FileDto Parse(string json)
        {
            JsonObject obj = JsonNode.Parse(json) as JsonObject ?? new JsonObject();
            var dto = new FileDto
            {
                Theme = Str(obj, "Theme", "Dark"),
                Language = Str(obj, "Language", "en"),
                StripGps = Flag(obj, "StripGps"),
                SecondaryDestination = Str(obj, "SecondaryDestination"),
                CreatorStamp = Str(obj, "CreatorStamp"),
                CopyrightStamp = Str(obj, "CopyrightStamp"),
                WriteChecksumManifest = Flag(obj, "WriteChecksumManifest", true),
                WriteXmpSidecar = Flag(obj, "WriteXmpSidecar"),
                WatchFolder = Str(obj, "WatchFolder"),
                TimeZoneMode = Str(obj, "TimeZoneMode", "CameraAsIs"),
                BatchRenameBase = Str(obj, "BatchRenameBase", "Shoot"),
                EjectAfterImport = Flag(obj, "EjectAfterImport"),
                DestinationPreset = Str(obj, "DestinationPreset", "Custom"),
                DuplicatePolicy = Str(obj, "DuplicatePolicy", "Suffix"),
                VerificationMode = Str(obj, "VerificationMode", "Fast"),
                GroupRawJpeg = Flag(obj, "GroupRawJpeg"),
                ExpandStacked = Flag(obj, "ExpandStacked"),
                ConfirmBeforeImport = Flag(obj, "ConfirmBeforeImport"),
                DeleteAfterImport = Flag(obj, "DeleteAfterImport"),
                ThumbnailSize = Num(obj, "ThumbnailSize"),
                TimeBetweenShootsHours = Int(obj, "TimeBetweenShootsHours"),
                FilterFileType = Str(obj, "FilterFileType", "All"),
                PreferAdb = FlagOpt(obj, "PreferAdb"),
                AllGroupsExpanded = FlagOpt(obj, "AllGroupsExpanded"),
            };
            PrefsLayout.Read(obj, dto);
            return dto;
        }

        private static string Str(JsonObject obj, string name, string fallback = "")
        {
            if (obj[name] is JsonValue value && value.TryGetValue(out string? text) && !string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            string? raw = obj[name]?.ToString();
            return string.IsNullOrWhiteSpace(raw) ? fallback : raw.Trim('"');
        }

        private static bool Flag(JsonObject obj, string name, bool fallback = false) =>
            obj[name] is JsonValue value && value.TryGetValue(out bool flag) ? flag : fallback;

        private static bool? FlagOpt(JsonObject obj, string name) =>
            obj[name] is JsonValue value && value.TryGetValue(out bool flag) ? flag : null;

        private static int Int(JsonObject obj, string name)
        {
            if (obj[name] is not JsonValue value)
            {
                return 0;
            }

            if (value.TryGetValue(out int i))
            {
                return i;
            }

            return value.TryGetValue(out double d) ? (int)d : 0;
        }

        private static double Num(JsonObject obj, string name)
        {
            if (obj[name] is not JsonValue value)
            {
                return 0;
            }

            if (value.TryGetValue(out double d))
            {
                return d;
            }

            return value.TryGetValue(out int i) ? i : 0;
        }
    }
}
