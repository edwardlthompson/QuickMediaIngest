#nullable enable
using System;
using System.IO;
using System.Threading;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core;

namespace QuickMediaIngest.Thumbnails.Wpf
{
    public partial class ThumbnailService
    {
        private DecodedThumbnail? GetThumbnailCore(
            string filePath,
            ThumbnailHints? hints,
            string cachePath,
            bool skipMagick)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            bool isRaw = MediaExtensions.IsRawExtension(ext);
            bool isVideo = MediaExtensions.IsVideoExtension(ext);
            DecodedThumbnail? thumb = null;

            if (ext is ".jpg" or ".jpeg")
            {
                try
                {
                    thumb = ExifThumbnailReader.TryGetExifThumbnail(filePath, _logger);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "EXIF embedded thumbnail read failed for {FilePath}.", filePath);
                }
            }

            if (thumb == null && isRaw)
            {
                if (TryGetSiblingRenderedPath(filePath, out string siblingRenderedPath))
                {
                    try
                    {
                        thumb = GetThumbnail(siblingRenderedPath, hints);
                    }
                    catch
                    {
                        // Continue to shell-based RAW thumbnail if sibling lookup fails.
                    }
                }

                if (thumb != null)
                {
                    return thumb;
                }

                int deferMs = hints?.DeferRawShellMilliseconds ?? 0;
                if (deferMs > 0)
                {
                    Thread.Sleep(deferMs);
                }

                try
                {
                    thumb = EncodeToDecodedThumbnail(
                        ShellThumbnailInterop.TryGetShellImage(filePath, 512, thumbnailOnly: false));
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "RAW shell preview extraction failed for {FilePath}.", filePath);
                }
            }

            if (thumb == null && !isRaw)
            {
                try
                {
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnLoad);
                    if (decoder.Frames.Count > 0)
                    {
                        thumb = EncodeToDecodedThumbnail(CreateResizedThumbnail(decoder.Frames[0], 240));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "WPF bitmap decode failed for {FilePath}.", filePath);
                }
            }

            if (thumb == null)
            {
                try
                {
                    BitmapSource? shell = ShellThumbnailInterop.TryGetShellImage(
                        filePath, isRaw || isVideo ? 512 : 240, thumbnailOnly: true);
                    if (shell == null && isVideo)
                    {
                        shell = ShellThumbnailInterop.TryGetShellImage(filePath, 512, thumbnailOnly: false);
                    }

                    thumb = EncodeToDecodedThumbnail(shell);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Shell thumbnail extraction failed for {FilePath}.", filePath);
                }
            }

            if (thumb == null && !skipMagick && !isVideo)
            {
                try
                {
                    thumb = TryGetMagickThumbnail(filePath, isRaw ? 320 : 240);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Magick thumbnail extraction failed for {FilePath}.", filePath);
                }
            }

            if (thumb != null)
            {
                TrySaveThumbnailCache(cachePath, thumb);
                return thumb;
            }

            _logger.LogWarning("All thumbnail decode paths failed for {FilePath}.", filePath);
            return null;
        }

        private static void TrySaveThumbnailCache(string cachePath, DecodedThumbnail thumb)
        {
            try
            {
                ThumbnailDiskCache.TrySave(thumb, cachePath);
            }
            catch
            {
                // Ignore cache write errors.
            }
        }

        private static DecodedThumbnail? TryGetMagickThumbnail(string filePath, int decodePixelWidth) =>
            MagickThumbnailDecoder.TryGetThumbnail(filePath, decodePixelWidth);

        private static DecodedThumbnail? EncodeToDecodedThumbnail(BitmapSource? source)
        {
            if (source == null)
            {
                return null;
            }

            try
            {
                var encoder = new JpegBitmapEncoder { QualityLevel = 90 };
                encoder.Frames.Add(BitmapFrame.Create(source));
                using var ms = new MemoryStream();
                encoder.Save(ms);
                byte[] bytes = ms.ToArray();
                int width = source.PixelWidth;
                int height = source.PixelHeight;
                if (JpegSofDimensionParser.TryGetDimensions(bytes, out int jw, out int jh))
                {
                    width = jw;
                    height = jh;
                }

                var decoded = new DecodedThumbnail(bytes, width, height);
                return ThumbnailPreviewValidator.IsAcceptable(decoded) ? decoded : null;
            }
            catch
            {
                return null;
            }
        }

        private static bool TryGetSiblingRenderedPath(string rawPath, out string siblingPath)
        {
            siblingPath = string.Empty;
            try
            {
                string basePath = Path.Combine(
                    Path.GetDirectoryName(rawPath) ?? string.Empty,
                    Path.GetFileNameWithoutExtension(rawPath));

                string[] candidates =
                {
                    basePath + ".jpg",
                    basePath + ".jpeg",
                    basePath + ".heic",
                    basePath + ".heif",
                    basePath + ".JPG",
                    basePath + ".JPEG",
                    basePath + ".HEIC",
                    basePath + ".HEIF",
                };

                foreach (string candidate in candidates)
                {
                    if (File.Exists(candidate))
                    {
                        siblingPath = candidate;
                        return true;
                    }
                }
            }
            catch
            {
                // Ignore sibling matching errors.
            }

            return false;
        }

        private static BitmapImage CreateResizedThumbnail(BitmapFrame frame, int decodePixelWidth)
        {
            byte[] bytes;
            using (var memoryStream = new MemoryStream())
            {
                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(frame);
                encoder.Save(memoryStream);
                bytes = memoryStream.ToArray();
            }

            using var pixelStream = new MemoryStream(bytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.DecodePixelWidth = Math.Max(120, decodePixelWidth);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = pixelStream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
    }
}
