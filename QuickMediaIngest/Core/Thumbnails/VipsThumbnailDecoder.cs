#nullable enable
using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core
{
    /// <summary>Optional libvips shrink-on-load decode; falls back when native libs are unavailable.</summary>
    internal static class VipsThumbnailDecoder
    {
        private static bool? _isAvailable;

        public static DecodedThumbnail? TryGetThumbnail(string filePath, int decodePixelWidth, ILogger? logger = null)
        {
            if (!IsAvailable())
            {
                return null;
            }

            try
            {
                using var image = NetVips.Image.Thumbnail(filePath, Math.Max(120, decodePixelWidth));
                using var memoryStream = new MemoryStream();
                image.WriteToStream(memoryStream, ".jpg");
                byte[] jpegBytes = memoryStream.ToArray();
                if (jpegBytes.Length == 0)
                {
                    return null;
                }

                var thumb = new DecodedThumbnail(jpegBytes, image.Width, image.Height);
                return ThumbnailPreviewValidator.IsAcceptable(thumb) ? thumb : null;
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "libvips thumbnail failed for {FilePath}.", filePath);
                return null;
            }
        }

        private static bool IsAvailable()
        {
            if (_isAvailable.HasValue)
            {
                return _isAvailable.Value;
            }

            try
            {
                _ = NetVips.NetVips.Version(0);
                _isAvailable = true;
            }
            catch (DllNotFoundException)
            {
                _isAvailable = false;
            }
            catch (TypeInitializationException)
            {
                _isAvailable = false;
            }
            catch
            {
                _isAvailable = false;
            }

            return _isAvailable.Value;
        }
    }
}
