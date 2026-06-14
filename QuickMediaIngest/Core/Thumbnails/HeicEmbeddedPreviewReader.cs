#nullable enable
using System;
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core
{
    /// <summary>Extract embedded JPEG segments from partial HEIC/HEIF buffers before full decode.</summary>
    internal static class HeicEmbeddedPreviewReader
    {
        public static BitmapSource? TryExtractFromFile(string filePath, ILogger? logger = null)
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

        internal static BitmapSource? TryExtractJpegSegment(byte[] data, ILogger? logger = null)
        {
            if (data.Length < 4)
            {
                return null;
            }

            int bestStart = -1;
            int bestLength = 0;

            for (int i = 0; i < data.Length - 1; i++)
            {
                if (data[i] != 0xFF || data[i + 1] != 0xD8)
                {
                    continue;
                }

                int end = FindJpegEnd(data, i + 2);
                if (end < 0)
                {
                    continue;
                }

                int length = end - i;
                if (length > bestLength)
                {
                    bestStart = i;
                    bestLength = length;
                }
            }

            if (bestStart < 0 || bestLength < 2048)
            {
                return null;
            }

            try
            {
                using var ms = new MemoryStream(data, bestStart, bestLength, writable: false);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();
                return ThumbnailPreviewValidator.IsAcceptable(bitmap) ? bitmap : null;
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "HEIC embedded JPEG segment decode failed.");
                return null;
            }
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
