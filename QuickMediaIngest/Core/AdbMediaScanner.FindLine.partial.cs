#nullable enable
using System;

namespace QuickMediaIngest.Core
{
    public sealed partial class AdbMediaScanner
    {
        /// <summary>
        /// Device Unix epoch → local wall clock. Missing mtime → per-file local Now (never a shared UtcNow batch stamp).
        /// </summary>
        internal static DateTime ResolveDateTaken(long mtimeUnixSeconds)
        {
            if (mtimeUnixSeconds > 0 && IsPlausibleUnixEpoch(mtimeUnixSeconds))
            {
                return DateTimeOffset.FromUnixTimeSeconds(mtimeUnixSeconds).LocalDateTime;
            }

            return DateTime.Now;
        }

        internal static bool IsPlausibleUnixEpoch(long seconds) =>
            seconds >= 946684800L && seconds <= 4102444800L; // 2000-01-01 .. 2100-01-01

        /// <summary>
        /// Parses <c>find</c> path-only lines or <c>stat -c '%n|%s'</c> / <c>%n|%s|%Y</c> lines.
        /// </summary>
        internal static bool TryParseFindLine(string line, out string devicePath, out long fileSize) =>
            TryParseFindLine(line, out devicePath, out fileSize, out _);

        internal static bool TryParseFindLine(
            string line,
            out string devicePath,
            out long fileSize,
            out long mtimeUnixSeconds)
        {
            devicePath = string.Empty;
            fileSize = 0;
            mtimeUnixSeconds = 0;
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return false;
            }

            // Prefer '|' (portable across toybox stat -c); then real tab; then space.
            foreach (char sep in new[] { '|', '\t', ' ' })
            {
                if (TryParseSeparatedFindLine(trimmed, sep, out devicePath, out fileSize, out mtimeUnixSeconds))
                {
                    return true;
                }
            }

            devicePath = trimmed;
            return true;
        }

        private static bool TryParseSeparatedFindLine(
            string trimmed,
            char sep,
            out string devicePath,
            out long fileSize,
            out long mtimeUnixSeconds)
        {
            devicePath = string.Empty;
            fileSize = 0;
            mtimeUnixSeconds = 0;

            int last = trimmed.LastIndexOf(sep);
            if (last <= 0 || last >= trimmed.Length - 1)
            {
                return false;
            }

            // Three fields: path|size|mtime
            int secondLast = trimmed.LastIndexOf(sep, last - 1);
            if (secondLast > 0
                && long.TryParse(trimmed[(last + 1)..], out long mt)
                && IsPlausibleUnixEpoch(mt)
                && long.TryParse(trimmed[(secondLast + 1)..last], out long size3)
                && size3 >= 0)
            {
                string path3 = trimmed[..secondLast].Trim();
                if (!string.IsNullOrEmpty(path3) && path3.Contains('/', StringComparison.Ordinal))
                {
                    devicePath = path3;
                    fileSize = size3;
                    mtimeUnixSeconds = mt;
                    return true;
                }
            }

            // Two fields: path|size
            if (!long.TryParse(trimmed[(last + 1)..], out long size) || size < 0)
            {
                return false;
            }

            string pathPart = trimmed[..last].Trim();
            if (string.IsNullOrEmpty(pathPart) || !pathPart.Contains('/', StringComparison.Ordinal))
            {
                return false;
            }

            devicePath = pathPart;
            fileSize = size;
            return true;
        }
    }
}
