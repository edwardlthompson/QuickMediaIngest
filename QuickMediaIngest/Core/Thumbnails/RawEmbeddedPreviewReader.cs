#nullable enable
using System;
using System.IO;

namespace QuickMediaIngest.Core
{
    /// <summary>Embedded JPEG inside CR2/NEF/ARW without demosaicing the sensor data.</summary>
    internal static class RawEmbeddedPreviewReader
    {
        private const int PrefixBytes = 8 * 1024 * 1024;

        public static DecodedThumbnail? TryExtract(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)
                || !File.Exists(filePath)
                || !MediaExtensions.IsRawExtension(Path.GetExtension(filePath)))
            {
                return null;
            }

            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                int n = (int)Math.Min(PrefixBytes, fs.Length);
                if (n < 2048)
                {
                    return null;
                }

                byte[] prefix = new byte[n];
                int read = fs.Read(prefix, 0, n);
                if (read < 2048)
                {
                    return null;
                }

                if (read < prefix.Length)
                {
                    Array.Resize(ref prefix, read);
                }

                return HeicEmbeddedPreviewReader.TryExtractJpegSegment(prefix);
            }
            catch
            {
                return null;
            }
        }
    }
}
