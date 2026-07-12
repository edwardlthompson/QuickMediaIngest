#nullable enable
using System;
using System.IO;
using System.Linq;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Core.Logging
{
    /// <summary>
    /// Redacts sensitive path prefixes for logs (PRIVACY.md: avoid full local paths where possible).
    /// </summary>
    public static class LogPathSanitizer
    {
        public enum PathKind
        {
            Local,
            AppData,
            FtpRemote
        }

        public static string ForLog(string? path, PathKind kind) => kind switch
        {
            PathKind.AppData => AppData(path),
            PathKind.FtpRemote => FtpRemote(path),
            _ => Local(path)
        };

        /// <summary>Keep drive + last <paramref name="tailSegments"/> segments; redact the middle.</summary>
        public static string Local(string? path, int tailSegments = 2)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "(empty)";
            }

            try
            {
                string trimmed = path.Trim();
                string? root = Path.GetPathRoot(trimmed);
                string[] segments = trimmed
                    .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

                // UNC: \\server\share\... — first segment is server, skip root handling
                bool isUnc = trimmed.StartsWith(@"\\", StringComparison.Ordinal) ||
                             trimmed.StartsWith("//", StringComparison.Ordinal);

                if (segments.Length == 0)
                {
                    return root ?? trimmed;
                }

                // Drop drive letter from segments when Path.GetPathRoot left it in (e.g. "C:")
                int start = 0;
                if (!isUnc && !string.IsNullOrEmpty(root) &&
                    segments[0].Equals(root.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase))
                {
                    start = 1;
                }

                int usable = segments.Length - start;
                if (usable <= tailSegments)
                {
                    return trimmed;
                }

                string tail = string.Join(
                    Path.DirectorySeparatorChar.ToString(),
                    segments.Skip(segments.Length - Math.Max(1, tailSegments)));

                string prefix = isUnc
                    ? @"\\" + segments[0]
                    : (root ?? string.Empty).TrimEnd('\\', '/');

                if (string.IsNullOrEmpty(prefix))
                {
                    return "..." + Path.DirectorySeparatorChar + tail;
                }

                return prefix + Path.DirectorySeparatorChar + "..." + Path.DirectorySeparatorChar + tail;
            }
            catch
            {
                return "(path)";
            }
        }

        /// <summary>Keep the app folder and file name under QuickMediaIngest.</summary>
        public static string AppData(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "(empty)";
            }

            try
            {
                string normalized = path.Replace('/', Path.DirectorySeparatorChar);
                const string marker = "QuickMediaIngest";
                int idx = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    return "..." + Path.DirectorySeparatorChar + normalized[idx..];
                }

                return Local(path, tailSegments: 2);
            }
            catch
            {
                return "(path)";
            }
        }

        /// <summary>Normalize and keep the last <paramref name="maxSegments"/> FTP path segments.</summary>
        public static string FtpRemote(string? path, int maxSegments = 4)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "(empty)";
            }

            try
            {
                string normalized = FtpPathNormalizer.Normalize(path);
                string[] segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length <= maxSegments)
                {
                    return normalized;
                }

                return "/.../" + string.Join("/", segments.TakeLast(maxSegments));
            }
            catch
            {
                return "(path)";
            }
        }
    }
}
