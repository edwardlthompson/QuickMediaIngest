#nullable enable
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
        public const long HeicBytes = 2 * 1024 * 1024;
        public const long VideoBytes = 4 * 1024 * 1024;
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
    }
}
