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
            bool isRaw = MediaExtensions.IsRawExtension(ext);
            bool isVideo = MediaExtensions.IsVideoExtension(ext);

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
                // Complete HEIC: Magick first — naive FF D8..FF D9 scans hit BMFF false positives.
                if (mode == FtpPreviewDecodeMode.CompleteFile)
                {
                    DecodedThumbnail? heicMagick = Accept(MagickThumbnailDecoder.TryGetThumbnail(tempPath, 240));
                    if (heicMagick != null)
                    {
                        return heicMagick;
                    }
                }

                DecodedThumbnail? embedded = Accept(HeicEmbeddedPreviewReader.TryExtractFromFile(tempPath, logger));
                if (embedded != null)
                {
                    return embedded;
                }

                if (mode == FtpPreviewDecodeMode.TieredPartial)
                {
                    return null;
                }

                if (mode == FtpPreviewDecodeMode.TieredFinalCap)
                {
                    return null;
                }
            }

            if (mode == FtpPreviewDecodeMode.TieredPartial)
            {
                return null;
            }

            // Partial RAW/video: never Magick on capped buffers.
            if (mode == FtpPreviewDecodeMode.TieredFinalCap && (isRaw || isVideo))
            {
                return null;
            }

            if (mode == FtpPreviewDecodeMode.TieredFinalCap)
            {
                // JPEG/PNG and similar stills only.
                return Accept(MagickThumbnailDecoder.TryGetThumbnail(tempPath, 240));
            }

            // CompleteFile video: Shell first (industry standard), optional ffmpeg.
            if (isVideo)
            {
                DecodedThumbnail? shellVideo = Accept(thumbnailService.GetThumbnail(tempPath, new ThumbnailHints
                {
                    DeferRawShellMilliseconds = hints?.DeferRawShellMilliseconds ?? 0,
                    IsPartialPreview = false,
                }));
                if (shellVideo != null)
                {
                    return shellVideo;
                }

                return Accept(FfmpegVideoThumbnailDecoder.TryGetThumbnail(tempPath, logger));
            }

            // CompleteFile stills
            DecodedThumbnail? magick = Accept(MagickThumbnailDecoder.TryGetThumbnail(tempPath, isRaw ? 320 : 240));
            if (magick != null)
            {
                return magick;
            }

            DecodedThumbnail? vips = Accept(VipsThumbnailDecoder.TryGetThumbnail(tempPath, isRaw ? 320 : 240, logger));
            if (vips != null)
            {
                return vips;
            }

            ThumbnailHints completeHints = new()
            {
                DeferRawShellMilliseconds = hints?.DeferRawShellMilliseconds ?? 0,
                IsPartialPreview = false,
            };
            return Accept(thumbnailService.GetThumbnail(tempPath, completeHints));
        }

        private static DecodedThumbnail? Accept(DecodedThumbnail? thumb) =>
            ThumbnailPreviewValidator.IsAcceptable(thumb) ? thumb : null;
    }
}
