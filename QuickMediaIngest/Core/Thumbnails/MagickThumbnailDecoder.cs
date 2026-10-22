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
                // Incomplete JPEG sources Magick-fill unread MCUs with gray — refuse them here.
                if (MediaExtensions.IsJpegFamily(ext) && !JpegFileLooksComplete(filePath))
                {
                    return null;
                }

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

                    var decoded = new DecodedThumbnail(jpegBytes, (int)image.Width, (int)image.Height);
                    // RAW Magick can invent green/magenta MCU fills; complete JPEGs are gated above.
                    if (MediaExtensions.IsRawExtension(ext)
                        && !ThumbnailPreviewValidator.IsAcceptable(decoded))
                    {
                        return null;
                    }

                    return decoded;
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

        internal static bool JpegFileLooksComplete(string filePath)
        {
            try
            {
                var info = new FileInfo(filePath);
                if (!info.Exists || info.Length < 4)
                {
                    return false;
                }

                // Read head + tail only — avoid loading multi-MB previews twice.
                int tailLen = (int)Math.Min(info.Length, 64 * 1024);
                byte[] buf = new byte[Math.Min(info.Length, 4 + tailLen)];
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                _ = fs.Read(buf, 0, 4);
                if (buf[0] != 0xFF || buf[1] != 0xD8)
                {
                    return false;
                }

                if (info.Length <= buf.Length)
                {
                    fs.Position = 0;
                    _ = fs.Read(buf, 0, (int)info.Length);
                    return ThumbnailPreviewValidator.HasJpegStartAndEndMarkers(buf[..(int)info.Length]);
                }

                byte[] tail = new byte[tailLen];
                fs.Position = info.Length - tailLen;
                _ = fs.Read(tail, 0, tailLen);
                // Synthesize SOI + tail so the shared EOI scanner can run.
                byte[] probe = new byte[2 + tailLen];
                probe[0] = 0xFF;
                probe[1] = 0xD8;
                Buffer.BlockCopy(tail, 0, probe, 2, tailLen);
                return ThumbnailPreviewValidator.HasJpegStartAndEndMarkers(probe);
            }
            catch
            {
                return false;
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
