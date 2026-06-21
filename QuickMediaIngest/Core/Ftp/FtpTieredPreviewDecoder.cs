#nullable enable
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Core
{
    /// <summary>Decode paths for tiered FTP preview byte budgets.</summary>
    internal static class FtpTieredPreviewDecoder
    {
        internal static BitmapSource? TryDecodeDownloaded(
            string fileName,
            string tempPath,
            ThumbnailHints? hints,
            IThumbnailService thumbnailService,
            ILogger logger,
            FtpPreviewDecodeMode mode = FtpPreviewDecodeMode.TieredPartial)
        {
            string ext = Path.GetExtension(fileName).ToLowerInvariant();

            if (ext is ".jpg" or ".jpeg")
            {
                BitmapSource? exif = Accept(ExifThumbnailReader.TryGetExifThumbnail(tempPath, logger));
                if (exif != null)
                {
                    return exif;
                }
            }

            if (ext is ".heic" or ".heif")
            {
                BitmapSource? embedded = Accept(HeicEmbeddedPreviewReader.TryExtractFromFile(tempPath, logger));
                if (embedded != null)
                {
                    return embedded;
                }
            }

            if (mode == FtpPreviewDecodeMode.TieredPartial)
            {
                return null;
            }

            BitmapSource? magick = Accept(MagickThumbnailDecoder.TryGetThumbnail(tempPath, 240));
            if (magick != null)
            {
                return magick;
            }

            if (mode == FtpPreviewDecodeMode.TieredFinalCap)
            {
                return null;
            }

            BitmapSource? vips = Accept(VipsThumbnailDecoder.TryGetThumbnail(tempPath, 240, logger));
            if (vips != null)
            {
                return vips;
            }

            return Accept(thumbnailService.GetThumbnail(tempPath, hints));
        }

        private static BitmapSource? Accept(BitmapSource? thumb) =>
            ThumbnailPreviewValidator.IsAcceptable(thumb) ? thumb : null;
    }
}
