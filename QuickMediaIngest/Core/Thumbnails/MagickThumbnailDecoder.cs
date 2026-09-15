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
                string ext = Path.GetExtension(filePath);
                MagickImage image = MediaExtensions.IsHeifFamily(ext)
                    ? OpenHeifPreferringThumbnail(filePath, ext)
                    : MediaExtensions.IsRawExtension(ext)
                        ? OpenRawPreferringThumbnail(filePath)
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

        public static DecodedThumbnail FitMaxEdge(DecodedThumbnail thumb, int maxEdge)
        {
            int edge = Math.Max(120, maxEdge);
            if (thumb.Width <= edge && thumb.Height <= edge)
            {
                return thumb;
            }

            try
            {
                using var image = new MagickImage(thumb.JpegBytes);
                image.AutoOrient();
                image.Thumbnail((uint)edge, (uint)edge);
                using var memoryStream = new MemoryStream();
                image.Write(memoryStream, MagickFormat.Jpeg);
                byte[] jpegBytes = memoryStream.ToArray();
                if (jpegBytes.Length == 0)
                {
                    return thumb;
                }

                return new DecodedThumbnail(jpegBytes, (int)image.Width, (int)image.Height);
            }
            catch
            {
                return thumb;
            }
        }

        private static MagickImage OpenRawPreferringThumbnail(string filePath)
        {
            try
            {
                var settings = new MagickReadSettings { Format = MagickFormat.Dng };
                settings.SetDefine(MagickFormat.Dng, "thumbnail", "true");
                return new MagickImage(filePath, settings);
            }
            catch
            {
                return new MagickImage(filePath);
            }
        }

        private static MagickImage OpenHeifPreferringThumbnail(string filePath, string ext)
        {
            MagickFormat format = ext.Equals(".avif", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".avifs", StringComparison.OrdinalIgnoreCase)
                ? MagickFormat.Avif
                : MagickFormat.Heic;
            try
            {
                var settings = new MagickReadSettings();
                settings.SetDefine(format, "thumbnail", "true");
                return new MagickImage(filePath, settings);
            }
            catch
            {
                return new MagickImage(filePath);
            }
        }
    }
}
