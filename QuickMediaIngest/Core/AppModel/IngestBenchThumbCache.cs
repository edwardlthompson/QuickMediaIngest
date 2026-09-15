#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Stable on-disk JPEG names so previews survive restart until import.</summary>
    internal static class IngestBenchThumbCache
    {
        public static string Identity(ImportItem item)
        {
            if (item.SourceId.StartsWith("adb:", StringComparison.OrdinalIgnoreCase))
            {
                return "adb|" + item.SourceId + "|" + item.SourcePath + "|" + item.FileSize;
            }

            return FileIdentity(item.SourcePath);
        }

        public static string FileIdentity(string path)
        {
            long length = 0;
            try
            {
                var info = new FileInfo(path);
                if (info.Exists)
                {
                    length = info.Length;
                }
            }
            catch
            {
                // Missing source — still a stable key.
            }

            return "v2|" + VolumeRelative(path) + "|" + length;
        }

        internal static string VolumeRelative(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string full;
            try
            {
                full = Path.GetFullPath(path);
            }
            catch
            {
                full = path;
            }

            string[] parts = full.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Equals("media", StringComparison.OrdinalIgnoreCase) && i + 2 < parts.Length)
                {
                    return string.Join("/", parts.Skip(i + 2));
                }
            }

            int take = Math.Min(3, parts.Length);
            return string.Join("/", parts.Skip(parts.Length - take));
        }

        public static string JpegPath(IAppPaths paths, string identity, int maxEdge) =>
            Path.Combine(IngestBenchThumbs.CacheDir(paths), Hash(identity + "|e" + maxEdge) + ".jpg");

        public static string? TryExisting(IAppPaths paths, string identity, int maxEdge)
        {
            string dest = JpegPath(paths, identity, maxEdge);
            try
            {
                if (!File.Exists(dest))
                {
                    return null;
                }

                long length = new FileInfo(dest).Length;
                if (length < 64 || length > MaxCacheBytes(maxEdge))
                {
                    return null;
                }

                return dest;
            }
            catch
            {
                return null;
            }
        }

        internal static long MaxCacheBytes(int maxEdge) =>
            Math.Max(200_000L, (long)Math.Max(120, maxEdge) * Math.Max(120, maxEdge));

        public static void Forget(IAppPaths paths, IEnumerable<ImportItem> items, int maxEdge = 256)
        {
            foreach (ImportItem item in items)
            {
                TryDelete(JpegPath(paths, Identity(item), maxEdge));
                if (!string.IsNullOrWhiteSpace(item.PreviewCachePath))
                {
                    TryDelete(item.PreviewCachePath);
                    item.PreviewCachePath = string.Empty;
                }
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Best effort.
            }
        }

        private static string Hash(string value) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }
}
