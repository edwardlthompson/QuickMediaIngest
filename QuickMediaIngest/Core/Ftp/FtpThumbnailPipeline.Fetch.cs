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
        private async Task<(DecodedThumbnail? Thumb, bool ViaAdb)> LoadWithTieredFetchAsync(
            FtpEndpoint endpoint,
            FtpThumbnailWorkItem workItem,
            string tempPath,
            ThumbnailHints? hints,
            bool useFluentFtp,
            SemaphoreSlim decodeGate,
            SemaphoreSlim fullDownloadGate,
            CancellationToken cancellationToken)
        {
            string ext = Path.GetExtension(workItem.FileName);
            DecodedThumbnail? deviceVideo = await TryLoadDeviceVideoThumbAsync(
                workItem,
                tempPath,
                hints,
                decodeGate,
                cancellationToken).ConfigureAwait(false);
            if (deviceVideo != null)
            {
                return (deviceVideo, true);
            }

            if (MediaExtensions.IsRawExtension(ext))
            {
                (DecodedThumbnail? sibling, bool siblingAdb) = await TryLoadSiblingPreviewAsync(
                    endpoint,
                    workItem,
                    tempPath,
                    hints,
                    useFluentFtp,
                    decodeGate,
                    cancellationToken);
                if (sibling != null)
                {
                    return (sibling, siblingAdb);
                }
            }

            foreach (string candidatePath in FtpMediaPathNormalizer.GetRetrCandidates(workItem.RemotePath))
            {
                if (await ShouldSkipMissingRemoteAsync(endpoint, candidatePath, cancellationToken).ConfigureAwait(false))
                {
                    continue;
                }

                string candidateName = Path.GetFileName(candidatePath);
                (DecodedThumbnail? preview, bool viaAdb) = await TryTieredDownloadAndDecodeAsync(
                    endpoint,
                    candidatePath,
                    candidateName,
                    workItem.FileSize,
                    tempPath,
                    hints,
                    useFluentFtp,
                    decodeGate,
                    cancellationToken);

                if (preview != null)
                {
                    return (preview, viaAdb);
                }
            }

            if (!ShouldTryFullDownload(workItem))
            {
                return (null, false);
            }

            foreach (string candidatePath in FtpMediaPathNormalizer.GetRetrCandidates(workItem.RemotePath))
            {
                if (await ShouldSkipMissingRemoteAsync(endpoint, candidatePath, cancellationToken).ConfigureAwait(false))
                {
                    continue;
                }

                bool full = false;
                bool viaAdb = false;
                await fullDownloadGate.WaitAsync(cancellationToken);
                try
                {
                    (full, viaAdb) = await TryCompleteFileDownloadAsync(
                        endpoint,
                        candidatePath,
                        workItem.FileSize,
                        tempPath,
                        useFluentFtp,
                        cancellationToken);
                }
                finally
                {
                    fullDownloadGate.Release();
                }

                if (!full)
                {
                    continue;
                }

                await decodeGate.WaitAsync(cancellationToken);
                try
                {
                    DecodedThumbnail? complete = _tieredLoader.TryDecodeDownloaded(
                        Path.GetFileName(candidatePath),
                        tempPath,
                        hints,
                        FtpPreviewDecodeMode.CompleteFile);
                    if (complete != null)
                    {
                        return (complete, viaAdb);
                    }
                }
                finally
                {
                    decodeGate.Release();
                }
            }

            return (null, false);
        }

        private async Task<DecodedThumbnail?> TryLoadDeviceVideoThumbAsync(
            FtpThumbnailWorkItem workItem,
            string tempPath,
            ThumbnailHints? hints,
            SemaphoreSlim decodeGate,
            CancellationToken cancellationToken)
        {
            string ext = Path.GetExtension(workItem.FileName);
            if (!MediaExtensions.IsVideoExtension(ext) ||
                _adbSession is not { } videoAdb ||
                _adbVideoThumbnailFetcher == null)
            {
                return null;
            }

            string jpegTemp = Path.ChangeExtension(tempPath, ".jpg");
            try
            {
                bool gotJpeg = await _adbVideoThumbnailFetcher.TryFetchVideoThumbJpegAsync(
                        videoAdb,
                        workItem.RemotePath,
                        jpegTemp,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (!gotJpeg)
                {
                    return null;
                }

                await decodeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    return _tieredLoader.TryDecodeDownloaded(
                        Path.GetFileName(jpegTemp),
                        jpegTemp,
                        hints,
                        FtpPreviewDecodeMode.CompleteFile);
                }
                finally
                {
                    decodeGate.Release();
                }
            }
            finally
            {
                TryDeleteTemp(jpegTemp);
            }
        }
    }
}
