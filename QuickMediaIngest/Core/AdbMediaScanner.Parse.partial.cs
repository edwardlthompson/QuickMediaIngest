#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    public sealed partial class AdbMediaScanner
    {
        private static List<ImportItem> ParseFindOutput(
            string mediaRootPrefix,
            string folder,
            bool includeSubfolders,
            string output,
            CancellationToken cancellationToken)
        {
            var items = new List<ImportItem>();
            using var reader = new StringReader(output);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!TryParseFindLine(line, out string devicePath, out long fileSize, out long mtimeUnix))
                {
                    continue;
                }

                if (!AdbAndroidPath.TryNormalizeRemote(devicePath, out string normalizedDevice))
                {
                    continue;
                }

                if (!TryToFtpStylePath(mediaRootPrefix, normalizedDevice, out string ftpPath))
                {
                    continue;
                }

                if (!includeSubfolders && IsNestedUnderFolder(folder, ftpPath))
                {
                    continue;
                }

                string name = Path.GetFileName(ftpPath);
                if (!MediaExtensions.IsMediaFile(name) || IsUnderTrash(ftpPath))
                {
                    continue;
                }

                string ext = Path.GetExtension(name);
                items.Add(new ImportItem
                {
                    FileName = name,
                    SourcePath = ftpPath,
                    FileSize = fileSize,
                    DateTaken = ResolveDateTaken(mtimeUnix),
                    IsVideo = MediaExtensions.IsVideoExtension(ext),
                    FileType = ext.TrimStart('.').ToUpperInvariant(),
                });
            }

            return items;
        }

        internal static bool TryToFtpStylePath(string mediaRootPrefix, string devicePath, out string ftpPath)
        {
            ftpPath = string.Empty;
            if (!AdbAndroidPath.TryNormalizeRemote(mediaRootPrefix, out string root) ||
                !AdbAndroidPath.TryNormalizeRemote(devicePath, out string full) ||
                !full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string remainder = full[root.Length..];
            if (string.IsNullOrEmpty(remainder))
            {
                return false;
            }

            ftpPath = remainder.StartsWith('/') ? remainder : "/" + remainder;
            return true;
        }

        internal static bool IsNestedUnderFolder(string folder, string ftpPath)
        {
            if (!AdbAndroidPath.TryNormalizeRemote(folder, out string root) ||
                !AdbAndroidPath.TryNormalizeRemote(ftpPath, out string path))
            {
                return true;
            }

            if (!path.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase) &&
                !path.Equals(root, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string relative = path.Length > root.Length ? path[(root.Length + 1)..] : string.Empty;
            return relative.Contains('/', StringComparison.Ordinal);
        }

        private static bool IsUnderTrash(string ftpPath)
        {
            foreach (string segment in ftpPath.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                if (MediaExtensions.IsAndroidTrashDirectory(segment))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
