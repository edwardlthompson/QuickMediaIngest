#nullable enable
using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core.Thumbnails
{
    public static class HeifPreviewDecoder
    {
        public static DecodedThumbnail? TryDecode(string filePath, int targetWidth = 320, ILogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return null;
            }

            // 1. Try embedded preview reader first
            var embedded = HeicEmbeddedPreviewReader.TryExtractFromFile(filePath, logger);
            if (embedded != null && ThumbnailPreviewValidator.IsAcceptable(embedded))
            {
                return embedded;
            }

            // 2. Try libvips
            var vipsThumb = VipsThumbnailDecoder.TryGetThumbnail(filePath, targetWidth, logger);
            if (vipsThumb != null && ThumbnailPreviewValidator.IsAcceptable(vipsThumb))
            {
                return vipsThumb;
            }

            return null;
        }
    }
}
