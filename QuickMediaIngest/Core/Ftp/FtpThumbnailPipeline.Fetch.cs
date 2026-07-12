#nullable enable
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Core
{
    internal sealed partial class FtpThumbnailPipeline
    {
        private async Task<DecodedThumbnail?> LoadWithTieredFetchAsync(
            FtpEndpoint endpoint,
            FtpThumbnailWorkItem workItem,
            string tempPath,
            ThumbnailHints? hints,
            bool useFluentFtp,
            SemaphoreSlim decodeGate,
            SemaphoreSlim fullDownloadGate,
            CancellationToken cancellationToken)
        {
            if (MediaExtensions.IsRawExtension(Path.GetExtension(workItem.FileName)))
            {
                DecodedThumbnail? sibling = await TryLoadSiblingPreviewAsync(
                    endpoint,
                    workItem,
                    tempPath,
                    hints,
                    useFluentFtp,
                    decodeGate,
                    cancellationToken);
                if (sibling != null)
                {
                    return sibling;
                }
            }

            DecodedThumbnail? preview = await TryTieredDownloadAndDecodeAsync(
                endpoint,
                workItem.RemotePath,
                workItem.FileName,
                tempPath,
                hints,
                useFluentFtp,
                decodeGate,
                cancellationToken);

            if (preview != null || !ShouldTryFullDownload(workItem))
            {
                return preview;
            }

            bool full = false;
            await fullDownloadGate.WaitAsync(cancellationToken);
            try
            {
                full = await _fileDownloader.TryDownloadAsync(
                    endpoint.Host,
                    endpoint.Port,
                    endpoint.User,
                    endpoint.Pass,
                    workItem.RemotePath,
                    tempPath,
                    45,
                    cancellationToken);
            }
            finally
            {
                fullDownloadGate.Release();
            }

            if (!full)
            {
                return null;
            }

            await decodeGate.WaitAsync(cancellationToken);
            try
            {
                return _tieredLoader.TryDecodeDownloaded(
                    workItem.FileName,
                    tempPath,
                    hints,
                    FtpPreviewDecodeMode.CompleteFile);
            }
            finally
            {
                decodeGate.Release();
            }
        }
    }
}
