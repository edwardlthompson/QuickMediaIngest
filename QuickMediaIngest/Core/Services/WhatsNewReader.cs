#nullable enable
using System;
using System.Collections.Generic;
using System.IO;

namespace QuickMediaIngest.Core.Services
{
    public static class WhatsNewReader
    {
        public static IReadOnlyList<string> ReadLatestHighlights(string? changelogPath = null, int maxItems = 10)
        {
            var highlights = new List<string>();
            string path = changelogPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CHANGELOG.md");

            if (!File.Exists(path))
            {
                // Try workspace root fallback
                string fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "CHANGELOG.md");
                if (File.Exists(fallback)) path = fallback;
                else return highlights;
            }

            try
            {
                var lines = File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
                    {
                        highlights.Add(trimmed.Substring(2).Trim());
                        if (highlights.Count >= maxItems) break;
                    }
                }
            }
            catch
            {
                // Best effort
            }

            return highlights;
        }
    }
}
