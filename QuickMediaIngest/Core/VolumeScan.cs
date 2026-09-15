#nullable enable
using System;
using System.Collections.Generic;
using System.IO;

namespace QuickMediaIngest.Core
{
    /// <summary>Shallow media-file list/count for Refresh (depth-capped).</summary>
    public static class VolumeScan
    {
        public const int DefaultMaxDepth = 4;
        public const int DefaultCap = 5000;

        public static int CountMedia(
            IEnumerable<string> roots,
            int maxDepth = DefaultMaxDepth,
            int cap = DefaultCap,
            IEnumerable<string>? excludeFolders = null) =>
            ListMedia(roots, maxDepth, cap, excludeFolders).Count;

        public static List<string> ListMedia(
            IEnumerable<string> roots,
            int maxDepth = DefaultMaxDepth,
            int cap = DefaultCap,
            IEnumerable<string>? excludeFolders = null)
        {
            var files = new List<string>();
            IEnumerable<string> skip = excludeFolders ?? Array.Empty<string>();
            foreach (string root in roots)
            {
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root) || files.Count >= cap)
                {
                    continue;
                }

                CollectDir(root, 0, maxDepth, cap, files, skip);
            }

            return files;
        }

        private static void CollectDir(string dir, int depth, int maxDepth, int cap, List<string> files, IEnumerable<string> skip)
        {
            if (files.Count >= cap || MediaExtensions.IsAndroidTrashDirectory(dir) || ScanExclusionMatcher.IsUnder(dir, skip))
            {
                return;
            }

            try
            {
                foreach (string file in Directory.EnumerateFiles(dir))
                {
                    if (MediaExtensions.IsMediaFile(file))
                    {
                        files.Add(file);
                        if (files.Count >= cap)
                        {
                            return;
                        }
                    }
                }

                if (depth >= maxDepth)
                {
                    return;
                }

                foreach (string child in Directory.EnumerateDirectories(dir))
                {
                    CollectDir(child, depth + 1, maxDepth, cap, files, skip);
                    if (files.Count >= cap)
                    {
                        return;
                    }
                }
            }
            catch (Exception)
            {
                // Unreadable mount.
            }
        }
    }
}
