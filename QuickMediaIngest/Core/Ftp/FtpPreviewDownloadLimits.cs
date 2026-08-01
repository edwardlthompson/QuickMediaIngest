#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuickMediaIngest.Core
{
    /// <summary>Byte caps and tiered budgets for capped FTP preview downloads by media type.</summary>
    public static class FtpPreviewDownloadLimits
    {
        public const long Tier64K = 64 * 1024;
        public const long Tier256K = 256 * 1024;
        public const long Tier512K = 512 * 1024;
        public const long ImageBytes = 512 * 1024;
        public const long HeicBytes = 12 * 1024 * 1024;
        public const long VideoBytes = 8 * 1024 * 1024;
        /// <summary>Max full-file pull for video grid thumbs when device JPEG thumb is unavailable.</summary>
        public const long VideoCompleteFallbackBytes = 256L * 1024 * 1024;
        public const long RawBytes = 2 * 1024 * 1024;

        private static readonly long[] BaseTiers = { Tier64K, Tier256K, Tier512K };

        public static long GetMaxPreviewBytes(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (MediaExtensions.IsVideoExtension(ext))
            {
                return VideoBytes;
            }

            if (MediaExtensions.IsRawExtension(ext))
            {
                return RawBytes;
            }

            if (ext is ".heic" or ".heif")
            {
                return HeicBytes;
            }

            return ImageBytes;
        }

        public static IReadOnlyList<long> GetPreviewByteTiers(string fileName)
        {
            long typeCap = GetMaxPreviewBytes(fileName);
            var tiers = new List<long>();
            foreach (long tier in BaseTiers)
            {
                if (tier <= typeCap)
                {
                    tiers.Add(tier);
                }
            }

            if (tiers.Count == 0 || tiers[^1] != typeCap)
            {
                tiers.Add(typeCap);
            }

            return tiers.Distinct().OrderBy(t => t).ToList();
        }

        /// <summary>
        /// Prefer a single full-file fetch when size is known and fits the type budget;
        /// skip tiny HEIC tiers when size is unknown (embeds are unreliable).
        /// </summary>
        public static IReadOnlyList<long> GetFetchTiers(string fileName, long knownFileSize)
        {
            IReadOnlyList<long> tiers = GetPreviewByteTiers(fileName);
            long typeCap = tiers[^1];

            if (knownFileSize > 0 && knownFileSize <= typeCap)
            {
                return new[] { knownFileSize };
            }

            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (knownFileSize <= 0 && ext is ".heic" or ".heif")
            {
                return new[] { HeicBytes };
            }

            // Large videos: skip truncated tiers (Shell cannot seek incomplete MP4).
            if (MediaExtensions.IsVideoExtension(ext) && knownFileSize > VideoBytes)
            {
                return Array.Empty<long>();
            }

            return tiers;
        }

        public static bool ShouldTryVideoCompleteFallback(long knownFileSize) =>
            knownFileSize > 0 && knownFileSize <= VideoCompleteFallbackBytes;
    }
}
