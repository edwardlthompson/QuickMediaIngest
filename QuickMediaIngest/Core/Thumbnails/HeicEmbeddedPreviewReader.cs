#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core
{
    /// <summary>Extract embedded JPEG segments from partial HEIC/HEIF buffers before full decode.</summary>
    internal static class HeicEmbeddedPreviewReader
    {
        public static DecodedThumbnail? TryExtractFromFile(string filePath, ILogger? logger = null)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }

                byte[] data = File.ReadAllBytes(filePath);
                return TryExtractJpegSegment(data, logger);
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "HEIC embedded preview read failed for {FilePath}.", filePath);
                return null;
            }
        }

        internal static DecodedThumbnail? TryExtractJpegSegment(byte[] data, ILogger? logger = null)
        {
            if (data.Length < 4)
            {
                return null;
            }

            // Longest first; require SOI + marker (FF D8 FF …) so BMFF noise is skipped.
            foreach ((int start, int length) in EnumerateJpegCandidates(data))
            {
                if (length < 2048 || start + 3 >= data.Length || data[start + 2] != 0xFF)
                {
                    continue;
                }

                try
                {
                    byte[] jpegBytes = new byte[length];
                    Buffer.BlockCopy(data, start, jpegBytes, 0, length);
                    DecodedThumbnail? thumb = JpegSofDimensionParser.TryCreate(jpegBytes);
                    if (thumb != null)
                    {
                        return thumb;
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogDebug(ex, "HEIC embedded JPEG segment decode failed.");
                }
            }

            return null;
        }

        private static IEnumerable<(int Start, int Length)> EnumerateJpegCandidates(byte[] data)
        {
            var found = new List<(int Start, int Length)>();
            for (int i = 0; i < data.Length - 1; i++)
            {
                if (data[i] != 0xFF || data[i + 1] != 0xD8)
                {
                    continue;
                }

                int end = FindJpegEnd(data, i + 2);
                if (end >= 0)
                {
                    found.Add((i, end - i));
                }
            }

            return found.OrderByDescending(c => c.Length);
        }

        private static int FindJpegEnd(byte[] data, int start)
        {
            for (int i = start; i < data.Length - 1; i++)
            {
                if (data[i] == 0xFF && data[i + 1] == 0xD9)
                {
                    return i + 2;
                }
            }

            return -1;
        }
    }
}
