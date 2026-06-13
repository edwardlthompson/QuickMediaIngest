#nullable enable
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace QuickMediaIngest.Core
{
    /// <summary>Parses Unix and DOS style FTP directory listing lines.</summary>
    public static class FtpListingParser
    {
        private static readonly Regex UnixListRegex = new(
            "^(?<type>[dl-])[rwxstST-]{9}\\s+\\d+\\s+\\S+\\s+\\S+\\s+(?<size>\\d+)\\s+(?<month>[A-Za-z]{3})\\s+(?<day>\\d{1,2})\\s+(?<timeyear>[0-9:]{4,5}|\\d{4})\\s+(?<name>.+)$",
            RegexOptions.Compiled);

        private static readonly Regex DosListRegex = new(
            "^(?<date>\\d{2}-\\d{2}-\\d{2})\\s+(?<time>\\d{2}:\\d{2}[AP]M)\\s+(?<dir><DIR>|\\d+)\\s+(?<name>.+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool TryParseListingLine(string line, string parentPath, out FtpListingEntry? entry)
        {
            entry = null;
            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            Match unixMatch = UnixListRegex.Match(line);
            if (unixMatch.Success)
            {
                string name = unixMatch.Groups["name"].Value.Trim();
                if (name is "." or "..")
                {
                    return false;
                }

                bool isDirectory = string.Equals(unixMatch.Groups["type"].Value, "d", StringComparison.OrdinalIgnoreCase);
                long.TryParse(unixMatch.Groups["size"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long size);
                DateTime modified = ParseUnixModified(
                    unixMatch.Groups["month"].Value,
                    unixMatch.Groups["day"].Value,
                    unixMatch.Groups["timeyear"].Value);

                entry = new FtpListingEntry
                {
                    Name = name,
                    FullPath = CombineRemotePath(parentPath, name),
                    IsDirectory = isDirectory,
                    Size = isDirectory ? 0 : size,
                    Modified = modified,
                };
                return true;
            }

            Match dosMatch = DosListRegex.Match(line);
            if (dosMatch.Success)
            {
                string name = dosMatch.Groups["name"].Value.Trim();
                if (name is "." or "..")
                {
                    return false;
                }

                bool isDirectory = string.Equals(dosMatch.Groups["dir"].Value, "<DIR>", StringComparison.OrdinalIgnoreCase);
                long size = 0;
                if (!isDirectory)
                {
                    long.TryParse(dosMatch.Groups["dir"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out size);
                }

                DateTime modified = DateTime.Now;
                string dateTimeText = $"{dosMatch.Groups["date"].Value} {dosMatch.Groups["time"].Value}";
                if (DateTime.TryParseExact(
                    dateTimeText,
                    "MM-dd-yy hh:mmtt",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out DateTime parsed))
                {
                    modified = parsed;
                }

                entry = new FtpListingEntry
                {
                    Name = name,
                    FullPath = CombineRemotePath(parentPath, name),
                    IsDirectory = isDirectory,
                    Size = isDirectory ? 0 : size,
                    Modified = modified,
                };
                return true;
            }

            return false;
        }

        public static string CombineRemotePath(string parent, string child)
        {
            string normalizedParent = NormalizeRemotePath(parent).TrimEnd('/');
            string normalizedChild = child.Replace("\\", "/").Trim('/');
            if (string.IsNullOrEmpty(normalizedChild))
            {
                return normalizedParent;
            }

            if (string.IsNullOrEmpty(normalizedParent) || normalizedParent == "/")
            {
                return "/" + normalizedChild;
            }

            return normalizedParent + "/" + normalizedChild;
        }

        public static string NormalizeRemotePath(string remotePath)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                return "/";
            }

            string normalized = remotePath.Trim().Replace("\\", "/");
            if (!normalized.StartsWith("/", StringComparison.Ordinal))
            {
                normalized = "/" + normalized;
            }

            return normalized;
        }

        private static DateTime ParseUnixModified(string month, string day, string timeOrYear)
        {
            string dayText = day.PadLeft(2, '0');
            if (timeOrYear.Contains(':'))
            {
                string text = $"{month} {dayText} {DateTime.Now.Year} {timeOrYear}";
                if (DateTime.TryParseExact(
                    text,
                    "MMM dd yyyy HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out DateTime parsedWithTime))
                {
                    return parsedWithTime;
                }
            }
            else
            {
                string text = $"{month} {dayText} {timeOrYear}";
                if (DateTime.TryParseExact(
                    text,
                    "MMM dd yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out DateTime parsedWithYear))
                {
                    return parsedWithYear;
                }
            }

            return DateTime.Now;
        }
    }
}
