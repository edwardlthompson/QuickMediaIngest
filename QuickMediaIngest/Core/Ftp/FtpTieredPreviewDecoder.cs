#nullable enable
using System.IO;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Core
{
    /// <summary>Decode paths for tiered FTP preview byte budgets.</summary>
    internal static class FtpTieredPreviewDecoder
    {
        internal static DecodedThumbnail? TryDecodeDownloaded(
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
                DecodedThumbnail? exif = Accept(ExifThumbnailReader.TryGetExifThumbnail(tempPath, logger));
                if (exif != null)
                {
                    return exif;
                }
            }

            if (ext is ".heic" or ".heif")
            {
                DecodedThumbnail? embedded = Accept(HeicEmbeddedPreviewReader.TryExtractFromFile(tempPath, logger));
                if (embedded != null)
                {
                    return embedded;
                }
            }

            if (mode == FtpPreviewDecodeMode.TieredPartial)
            {
                return null;
            }

            DecodedThumbnail? magick = Accept(MagickThumbnailDecoder.TryGetThumbnail(tempPath, 240));
            if (magick != null)
            {
                return magick;
            }

            if (mode == FtpPreviewDecodeMode.TieredFinalCap)
            {
                return null;
            }

            DecodedThumbnail? vips = Accept(VipsThumbnailDecoder.TryGetThumbnail(tempPath, 240, logger));
            if (vips != null)
            {
                return vips;
            }

            return Accept(thumbnailService.GetThumbnail(tempPath, hints));
        }

        private static DecodedThumbnail? Accept(DecodedThumbnail? thumb) =>
            ThumbnailPreviewValidator.IsAcceptable(thumb) ? thumb : null;
    }
}
