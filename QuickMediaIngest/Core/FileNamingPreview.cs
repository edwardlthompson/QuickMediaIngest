#nullable enable
using System;
using System.Collections.Generic;

namespace QuickMediaIngest.Core
{
    /// <summary>Live filename examples for the naming builder.</summary>
    public static class FileNamingPreview
    {
        private static readonly string[] Originals = { "img_0001", "img_0002", "img_0003" };

        public static IReadOnlyList<string> Examples(
            string? template,
            NamingParts parts,
            string? shootSample,
            bool lowercase)
        {
            string sep = string.IsNullOrWhiteSpace(parts.Separator) ? "_" : parts.Separator;
            string date = parts.DateFormat == "yyyyMMdd" ? "20260425" : "2026-04-25";
            string time = parts.TimeFormat switch
            {
                "HHmmss" => "195649",
                "HH-mm-ss-fff" => "19-56-49-123",
                "HHmmssfff" => "195649123",
                _ => "19-56-49",
            };
            string shoot = string.IsNullOrWhiteSpace(shootSample) ? "my-shoot" : shootSample.Trim();
            string t = string.IsNullOrWhiteSpace(template)
                ? "[Date]" + sep + "[ShootName]" + sep + "[Original]"
                : template;

            var list = new List<string>(Originals.Length);
            for (int i = 0; i < Originals.Length; i++)
            {
                string output = t
                    .Replace("[Date]", date, StringComparison.Ordinal)
                    .Replace("[Time]", time, StringComparison.Ordinal)
                    .Replace("[TimeMs]", "19-56-49-123", StringComparison.Ordinal)
                    .Replace("[YYYY]", "2026", StringComparison.Ordinal)
                    .Replace("[MM]", "04", StringComparison.Ordinal)
                    .Replace("[DD]", "25", StringComparison.Ordinal)
                    .Replace("[HH]", "19", StringComparison.Ordinal)
                    .Replace("[mm]", "56", StringComparison.Ordinal)
                    .Replace("[ss]", "49", StringComparison.Ordinal)
                    .Replace("[fff]", "123", StringComparison.Ordinal)
                    .Replace("[ShootName]", shoot, StringComparison.Ordinal)
                    .Replace("[Original]", Originals[i], StringComparison.Ordinal)
                    .Replace("[Sequence]", (i + 1).ToString("D4"), StringComparison.Ordinal)
                    .Replace("[Ext]", "jpg", StringComparison.Ordinal)
                    .Replace("__", "_", StringComparison.Ordinal)
                    .Replace("--", "-", StringComparison.Ordinal)
                    .Trim('_', '-');
                if (lowercase)
                {
                    output = output.ToLowerInvariant();
                }

                list.Add($"{output}.jpg");
            }

            return list;
        }

        public static IReadOnlyList<string> FolderExamples(string? template)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                return new[] { "Files go in the save location (no extra subfolder)." };
            }

            string sample = template
                .Replace("[Date]", "2026-04-25", StringComparison.Ordinal)
                .Replace("[YYYY]", "2026", StringComparison.Ordinal)
                .Replace("[MM]", "04", StringComparison.Ordinal)
                .Replace("[DD]", "25", StringComparison.Ordinal)
                .Replace("[ShootTitle]", "studio-day", StringComparison.Ordinal)
                .Replace("[Job]", "job-12", StringComparison.Ordinal)
                .Replace("[Client]", "acme", StringComparison.Ordinal)
                .Replace("[Camera]", "r5", StringComparison.Ordinal)
                .Replace("//", "/", StringComparison.Ordinal)
                .Trim('/');
            return new[] { sample };
        }

        public static IReadOnlyList<string> UnusedFileTokens(string? template)
        {
            string t = template ?? string.Empty;
            var list = new List<string>();
            foreach (string token in FileNamingBuilder.FileTokens)
            {
                if (token is "_" or "-" || !t.Contains(token, StringComparison.Ordinal))
                {
                    list.Add(token);
                }
            }

            return list;
        }

        public static IReadOnlyList<string> UsedFileTokens(string? template)
        {
            string t = template ?? string.Empty;
            var list = new List<string>();
            foreach (string token in FileNamingBuilder.FileTokens)
            {
                if (token is "_" or "-")
                {
                    continue;
                }

                if (t.Contains(token, StringComparison.Ordinal))
                {
                    list.Add(token);
                }
            }

            return list;
        }
    }
}
