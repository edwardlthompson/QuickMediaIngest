#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QuickMediaIngest.Core
{
    public class ThumbnailService : IThumbnailService
    {
        private readonly ILogger<ThumbnailService> _logger;

        public ThumbnailService(ILogger<ThumbnailService> logger)
        {
            _logger = logger;
        }

        public BitmapSource? GetThumbnail(string filePath) => GetThumbnail(filePath, null);

        public BitmapSource? GetThumbnail(string filePath, ThumbnailHints? hints)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            bool isRaw = MediaExtensions.IsRawExtension(ext);
            bool isVideo = MediaExtensions.IsVideoExtension(ext);
            string cachePath = ThumbnailDiskCache.GetCachePath(filePath);

            try
            {
                return ThumbnailDiskCache.TryLoad(cachePath);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Thumbnail cache read failed; will regenerate. Path: {CachePath}.", cachePath);
            }

            BitmapSource? thumb = null;

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
                    thumb = ShellThumbnailInterop.TryGetShellImage(filePath, 512, thumbnailOnly: false);
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
                        thumb = CreateResizedThumbnail(decoder.Frames[0], isRaw ? 320 : 240);
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
                    thumb = ShellThumbnailInterop.TryGetShellImage(filePath, isRaw || isVideo ? 512 : 240, thumbnailOnly: true);
                    if (thumb == null && isVideo)
                    {
                        thumb = ShellThumbnailInterop.TryGetShellImage(filePath, 512, thumbnailOnly: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Shell thumbnail extraction failed for {FilePath}.", filePath);
                }
            }

            if (thumb != null)
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

            return thumb;
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
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.DecodePixelWidth = Math.Max(120, decodePixelWidth);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;

            var memoryStream = new MemoryStream();
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(frame);
            encoder.Save(memoryStream);
            memoryStream.Position = 0;

            bitmap.StreamSource = memoryStream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
    }
}
