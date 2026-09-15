#nullable enable
using System;
using System.Collections.Generic;

namespace QuickMediaIngest.Core
{
    /// <summary>Prefix match for scan-exclusion folders (local paths, slash-insensitive).</summary>
    public static class ScanExclusionMatcher
    {
        public static bool IsUnder(string? path, IEnumerable<string>? prefixes)
        {
            if (string.IsNullOrWhiteSpace(path) || prefixes is null)
            {
                return false;
            }

            string haystack = Normalize(path);
            foreach (string prefix in prefixes)
            {
                if (string.IsNullOrWhiteSpace(prefix))
                {
                    continue;
                }

                string needle = Normalize(prefix);
                if (haystack.Equals(needle, StringComparison.OrdinalIgnoreCase)
                    || haystack.StartsWith(needle + "/", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static string Normalize(string path) =>
            path.Trim().Replace('\\', '/').TrimEnd('/');
    }
}
