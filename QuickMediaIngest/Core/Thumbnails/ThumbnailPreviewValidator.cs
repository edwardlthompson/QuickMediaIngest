#nullable enable
namespace QuickMediaIngest.Core
{
    /// <summary>Rejects corrupt or placeholder-sized decode results from partial FTP buffers.</summary>
    internal static class ThumbnailPreviewValidator
    {
        public const int MinPixelEdge = 32;

        public static bool IsAcceptable(DecodedThumbnail? thumb)
        {
            if (thumb == null)
            {
                return false;
            }

            if (thumb.JpegBytes == null || thumb.JpegBytes.Length == 0)
            {
                return false;
            }

            if (thumb.Width < MinPixelEdge || thumb.Height < MinPixelEdge)
            {
                return false;
            }

            return true;
        }
    }
}
