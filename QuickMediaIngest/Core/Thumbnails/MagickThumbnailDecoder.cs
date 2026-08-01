#nullable enable
using System;
using System.IO;
using ImageMagick;

namespace QuickMediaIngest.Core
{
    internal static class MagickThumbnailDecoder
    {
        public static DecodedThumbnail? TryGetThumbnail(string filePath, int decodePixelWidth)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                string ext = Path.GetExtension(filePath).ToLowerInvariant();
                MagickImage image = ext is ".heic" or ".heif"
                    ? OpenHeicPreferringThumbnail(filePath)
                    : new MagickImage(filePath);

                using (image)
                {
                    image.AutoOrient();
                    uint size = (uint)Math.Max(120, decodePixelWidth);
                    image.Thumbnail(size, size);

                    using var memoryStream = new MemoryStream();
                    image.Write(memoryStream, MagickFormat.Jpeg);
                    byte[] jpegBytes = memoryStream.ToArray();
                    if (jpegBytes.Length == 0)
                    {
                        return null;
                    }

                    return new DecodedThumbnail(jpegBytes, (int)image.Width, (int)image.Height);
                }
            }
            catch
            {
                return null;
            }
        }

        private static MagickImage OpenHeicPreferringThumbnail(string filePath)
        {
            try
            {
                var settings = new MagickReadSettings();
                settings.SetDefine(MagickFormat.Heic, "thumbnail", "true");
                return new MagickImage(filePath, settings);
            }
            catch
            {
                return new MagickImage(filePath);
            }
        }
    }
}
