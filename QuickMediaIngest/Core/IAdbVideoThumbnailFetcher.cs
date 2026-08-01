#nullable enable
using System.Threading;
using System.Threading.Tasks;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Fetches a small device-generated JPEG video thumbnail (MediaStore / on-device extract),
    /// analogous to MTP/WPD thumbs — not a full MP4 pull.
    /// </summary>
    public interface IAdbVideoThumbnailFetcher
    {
        Task<bool> TryFetchVideoThumbJpegAsync(
            AdbTransferSession session,
            string ftpRemotePath,
            string localJpegPath,
            CancellationToken cancellationToken);
    }
}
