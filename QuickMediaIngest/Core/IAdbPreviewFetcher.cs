#nullable enable
using System.Threading;
using System.Threading.Tasks;

namespace QuickMediaIngest.Core
{
    public interface IAdbPreviewFetcher
    {
        Task<bool> TryFetchCappedAsync(
            AdbTransferSession session,
            string ftpRemotePath,
            string localPath,
            long maxBytes,
            long knownFileSize,
            CancellationToken cancellationToken);
    }
}
