#nullable enable
using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core;

namespace QuickMediaIngest.Thumbnails.Wpf
{
    public partial class ThumbnailService : IThumbnailService
    {
        private readonly ILogger<ThumbnailService> _logger;

        public ThumbnailService(ILogger<ThumbnailService> logger)
        {
            _logger = logger;
        }

        public DecodedThumbnail? GetThumbnail(string filePath) => GetThumbnail(filePath, null);

        public DecodedThumbnail? GetThumbnail(string filePath, ThumbnailHints? hints)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            string cachePath = ThumbnailDiskCache.GetCachePath(filePath);
            try
            {
                DecodedThumbnail? cached = ThumbnailDiskCache.TryLoad(cachePath);
                if (cached != null)
                {
                    return cached;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Thumbnail cache read failed; will regenerate. Path: {CachePath}.", cachePath);
            }

            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            bool isRaw = MediaExtensions.IsRawExtension(ext);
            bool isVideo = MediaExtensions.IsVideoExtension(ext);

            if (!isVideo)
            {
                try
                {
                    DecodedThumbnail? vipsThumb = VipsThumbnailDecoder.TryGetThumbnail(filePath, isRaw ? 320 : 240, _logger);
                    if (vipsThumb != null)
                    {
                        TrySaveThumbnailCache(cachePath, vipsThumb);
                        return vipsThumb;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "libvips thumbnail extraction failed for {FilePath}.", filePath);
                }

                try
                {
                    DecodedThumbnail? magickThumb = TryGetMagickThumbnail(filePath, isRaw ? 320 : 240);
                    if (magickThumb != null)
                    {
                        TrySaveThumbnailCache(cachePath, magickThumb);
                        return magickThumb;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Magick thumbnail extraction failed for {FilePath}.", filePath);
                }
            }

            Dispatcher? dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null)
            {
                if (dispatcher.CheckAccess())
                {
                    return GetThumbnailCore(filePath, hints, cachePath, skipMagick: true);
                }

                return dispatcher.Invoke(
                    () => GetThumbnailCore(filePath, hints, cachePath, skipMagick: true),
                    DispatcherPriority.Background);
            }

            return StaRunner.Run(() => GetThumbnailCore(filePath, hints, cachePath, skipMagick: true));
        }
    }
}
