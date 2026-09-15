#nullable enable
using System;
using System.Collections.Generic;

namespace QuickMediaIngest.Core
{
    public readonly record struct NamingParts(
        bool Date,
        bool Time,
        bool Sequence,
        bool Shoot,
        bool Original,
        string DateFormat,
        string TimeFormat,
        string Separator);

    /// <summary>Preset/template/token helpers shared by WPF Settings and Avalonia.</summary>
    public static class FileNamingBuilder
    {
        public const string PresetRecommended = "Recommended (Date + Shoot + Original)";
        public const string PresetDateTime = "Date + Time + Shoot + Original";
        public const string PresetShootDate = "Shoot + Date + Original";
        public const string PresetCustom = "Custom";

        public static IReadOnlyList<string> Presets { get; } =
            new[] { PresetRecommended, PresetDateTime, PresetShootDate, PresetCustom };

        public static IReadOnlyList<string> DateFormats { get; } = new[] { "yyyy-MM-dd", "yyyyMMdd" };

        public static IReadOnlyList<string> TimeFormats { get; } =
            new[] { "HH-mm-ss", "HHmmss", "HH-mm-ss-fff", "HHmmssfff" };

        public static IReadOnlyList<string> Separators { get; } = new[] { "_", "-" };

        public static IReadOnlyList<string> FileTokens { get; } = new[]
        {
            "[Date]", "[Time]", "[TimeMs]", "[YYYY]", "[MM]", "[DD]", "[HH]", "[mm]", "[ss]", "[fff]",
            "[ShootName]", "[Original]", "[Sequence]", "[Ext]", "_", "-",
        };

        public static IReadOnlyList<string> FolderTokens { get; } =
            new[] { "[Date]", "[YYYY]", "[MM]", "[DD]", "[ShootTitle]", "[Job]", "[Client]", "[Camera]" };

        public static NamingParts DefaultParts { get; } = new(
            Date: true, Time: true, Sequence: false, Shoot: false, Original: true,
            DateFormat: "yyyy-MM-dd", TimeFormat: "HH-mm-ss", Separator: "_");

        public static NamingParts PresetParts(string preset, NamingParts current)
        {
            if (string.Equals(preset, PresetCustom, StringComparison.Ordinal))
            {
                return current;
            }

            bool time = string.Equals(preset, PresetDateTime, StringComparison.Ordinal);
            return current with { Date = true, Time = time, Sequence = false, Shoot = true, Original = true };
        }

        public static string BuildTemplate(NamingParts parts)
        {
            var chunks = new List<string>();
            if (parts.Date)
            {
                chunks.Add(parts.DateFormat == "yyyyMMdd" ? "[YYYY][MM][DD]" : "[Date]");
            }

            if (parts.Time)
            {
                chunks.Add(parts.TimeFormat switch
                {
                    "HHmmss" => "[HH][mm][ss]",
                    "HH-mm-ss-fff" => "[TimeMs]",
                    "HHmmssfff" => "[HH][mm][ss][fff]",
                    _ => "[Time]",
                });
            }

            if (parts.Sequence)
            {
                chunks.Add("[Sequence]");
            }

            if (parts.Shoot)
            {
                chunks.Add("[ShootName]");
            }

            if (parts.Original)
            {
                chunks.Add("[Original]");
            }

            if (chunks.Count == 0)
            {
                chunks.Add("[Original]");
            }

            string sep = string.IsNullOrEmpty(parts.Separator) ? "_" : parts.Separator;
            return string.Join(sep, chunks);
        }

        public static NamingParts SyncFromTemplate(string? template)
        {
            string t = template ?? string.Empty;
            return new NamingParts(
                Date: t.Contains("[Date]", StringComparison.Ordinal) ||
                      (t.Contains("[YYYY]", StringComparison.Ordinal) &&
                       t.Contains("[MM]", StringComparison.Ordinal) &&
                       t.Contains("[DD]", StringComparison.Ordinal)),
                Time: t.Contains("[Time]", StringComparison.Ordinal) ||
                      t.Contains("[TimeMs]", StringComparison.Ordinal) ||
                      (t.Contains("[HH]", StringComparison.Ordinal) &&
                       t.Contains("[mm]", StringComparison.Ordinal) &&
                       t.Contains("[ss]", StringComparison.Ordinal)),
                Sequence: t.Contains("[Sequence]", StringComparison.Ordinal),
                Shoot: t.Contains("[ShootName]", StringComparison.Ordinal),
                Original: t.Contains("[Original]", StringComparison.Ordinal),
                DateFormat: t.Contains("[YYYY][MM][DD]", StringComparison.Ordinal) ? "yyyyMMdd" : "yyyy-MM-dd",
                TimeFormat: t.Contains("[HH][mm][ss][fff]", StringComparison.Ordinal) ? "HHmmssfff"
                    : t.Contains("[TimeMs]", StringComparison.Ordinal) ? "HH-mm-ss-fff"
                    : t.Contains("[HH][mm][ss]", StringComparison.Ordinal) ? "HHmmss"
                    : "HH-mm-ss",
                Separator: t.Contains('-') && !t.Contains('_') ? "-" : "_");
        }

        public static string CoercePreset(string? preset, string template, NamingParts formats)
        {
            if (string.IsNullOrWhiteSpace(preset) ||
                string.Equals(preset, PresetCustom, StringComparison.Ordinal))
            {
                return PresetCustom;
            }

            string expected = BuildTemplate(PresetParts(preset, formats));
            return string.Equals(template, expected, StringComparison.Ordinal) ? preset : PresetCustom;
        }

        public static string InsertToken(string? template, string? token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return template ?? string.Empty;
            }

            string current = template ?? string.Empty;
            if (token[0] == '[' && token[^1] == ']' &&
                current.Contains(token, StringComparison.Ordinal))
            {
                return current;
            }

            if (string.IsNullOrWhiteSpace(current))
            {
                return token;
            }

            if (token is "_" or "-")
            {
                return current + token;
            }

            if (current.EndsWith('_') || current.EndsWith('-'))
            {
                return current + token;
            }

            string sep = current.Contains('-') && !current.Contains('_') ? "-" : "_";
            return current + sep + token;
        }

        public static string RemoveToken(string? template, string? token)
        {
            if (string.IsNullOrEmpty(template) || string.IsNullOrEmpty(token))
            {
                return template ?? string.Empty;
            }

            string next = template.Replace(token, string.Empty, StringComparison.Ordinal)
                .Replace("__", "_", StringComparison.Ordinal)
                .Replace("--", "-", StringComparison.Ordinal)
                .Trim('_', '-');
            return string.IsNullOrWhiteSpace(next) ? "[Original]" : next;
        }
    }
}
