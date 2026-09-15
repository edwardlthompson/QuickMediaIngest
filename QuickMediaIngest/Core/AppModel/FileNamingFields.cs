#nullable enable
using System;
using System.Collections.Generic;

namespace QuickMediaIngest.Core.AppModel
{
    public readonly record struct NamingField(string Token, string Label);

    public readonly record struct FolderParts(
        bool ShootTitle, bool Date, bool Year, bool Month, bool Day, bool Job, bool Client, bool Camera);

    /// <summary>Always-visible labeled fields for the filename/folder builders (not disappearing chips).</summary>
    public static class FileNamingFields
    {
        public static IReadOnlyList<NamingField> Folder { get; } = new NamingField[]
        {
            new("[ShootTitle]", "Shoot title"),
            new("[Date]", "Date"),
            new("[YYYY]", "Year"),
            new("[MM]", "Month"),
            new("[DD]", "Day"),
            new("[Job]", "Job"),
            new("[Client]", "Client"),
            new("[Camera]", "Camera"),
        };

        public static FolderParts SyncFromFolder(string? template)
        {
            string t = template ?? string.Empty;
            return new(
                t.Contains("[ShootTitle]", StringComparison.Ordinal),
                t.Contains("[Date]", StringComparison.Ordinal),
                t.Contains("[YYYY]", StringComparison.Ordinal),
                t.Contains("[MM]", StringComparison.Ordinal),
                t.Contains("[DD]", StringComparison.Ordinal),
                t.Contains("[Job]", StringComparison.Ordinal),
                t.Contains("[Client]", StringComparison.Ordinal),
                t.Contains("[Camera]", StringComparison.Ordinal));
        }

        public static string FileToken(string? field, NamingParts parts) => field switch
        {
            "Date" => parts.DateFormat == "yyyyMMdd" ? "[YYYY][MM][DD]" : "[Date]",
            "Time" => parts.TimeFormat switch
            {
                "HHmmss" => "[HH][mm][ss]",
                "HH-mm-ss-fff" => "[TimeMs]",
                "HHmmssfff" => "[HH][mm][ss][fff]",
                _ => "[Time]",
            },
            "Sequence" => "[Sequence]",
            "Shoot" => "[ShootName]",
            "Original" => "[Original]",
            _ => string.Empty,
        };

        public static string StripFileField(string? template, string? field)
        {
            string[] tokens = field switch
            {
                "Date" => new[] { "[YYYY][MM][DD]", "[Date]" },
                "Time" => new[] { "[HH][mm][ss][fff]", "[TimeMs]", "[HH][mm][ss]", "[Time]" },
                "Sequence" => new[] { "[Sequence]" },
                "Shoot" => new[] { "[ShootName]" },
                "Original" => new[] { "[Original]" },
                _ => Array.Empty<string>(),
            };
            return Strip(template, tokens, "[Original]");
        }

        public static string Strip(string? template, string token, bool emptyOk) =>
            Strip(template, new[] { token }, emptyOk ? string.Empty : "[Original]");

        public static string RewriteFileFormats(string? template, NamingParts parts)
        {
            string t = template ?? string.Empty;
            if (t.Length == 0)
            {
                return t;
            }

            string dateWant = FileToken("Date", parts);
            t = Swap(t, dateWant, "[YYYY][MM][DD]", "[Date]");
            string timeWant = FileToken("Time", parts);
            t = Swap(t, timeWant, "[HH][mm][ss][fff]", "[TimeMs]", "[HH][mm][ss]", "[Time]");
            char sep = string.IsNullOrEmpty(parts.Separator) ? '_' : parts.Separator[0];
            char other = sep == '-' ? '_' : '-';
            return t.Replace(other, sep);
        }

        private static string Swap(string template, string want, params string[] forms)
        {
            foreach (string form in forms)
            {
                if (!string.Equals(form, want, StringComparison.Ordinal) &&
                    template.Contains(form, StringComparison.Ordinal))
                {
                    return template.Replace(form, want, StringComparison.Ordinal);
                }
            }

            return template;
        }

        private static string Strip(string? template, string[] tokens, string emptyFallback)
        {
            string next = template ?? string.Empty;
            foreach (string token in tokens)
            {
                next = next.Replace(token, string.Empty, StringComparison.Ordinal);
            }

            next = next.Replace("__", "_", StringComparison.Ordinal)
                .Replace("--", "-", StringComparison.Ordinal)
                .Trim('_', '-');
            return string.IsNullOrWhiteSpace(next) ? emptyFallback : next;
        }
    }
}
