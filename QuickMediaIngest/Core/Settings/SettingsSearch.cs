#nullable enable
using System;
using System.Collections.Generic;

namespace QuickMediaIngest.Core.Settings
{
    /// <summary>Filter Preferences sections by a case-insensitive substring. WPF-free for both heads.</summary>
    public static class SettingsSearch
    {
        public static bool IsBlank(string? query) => string.IsNullOrWhiteSpace(query);

        public static bool Matches(string? query, params string?[] haystacks)
        {
            if (IsBlank(query))
            {
                return true;
            }

            string needle = query!.Trim();
            foreach (string? haystack in haystacks)
            {
                if (!string.IsNullOrEmpty(haystack)
                    && haystack.IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool AnyMatch(string? query, IReadOnlyList<string[]> sections)
        {
            if (IsBlank(query))
            {
                return true;
            }

            foreach (string[] section in sections)
            {
                if (Matches(query, section))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
