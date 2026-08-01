#nullable enable
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Core
{
    internal sealed partial class FtpThumbnailPipeline
    {
        private async Task<(bool Ok, bool ViaAdb)> TryCompleteFileDownloadAsync(
            FtpEndpoint endpoint,
            string remotePath,
            long knownFileSize,
            string tempPath,
            bool useFluentFtp,
            CancellationToken cancellationToken)
        {
            TryDeleteTemp(tempPath);

            // Prefer ADB full/capped pull — avoids slow FTP of multi‑MB HEIC/DNG/MP4.
            if (_adbSession is { } adb && _adbPreviewFetcher != null)
            {
                string fileName = Path.GetFileName(remotePath);
                bool isVideo = MediaExtensions.IsVideoExtension(Path.GetExtension(fileName));
                long maxPull = isVideo
                    ? FtpPreviewDownloadLimits.VideoCompleteFallbackBytes
                    : 40L * 1024 * 1024;
                long pullBudget = knownFileSize > 0
                    ? knownFileSize
                    : FtpPreviewDownloadLimits.GetMaxPreviewBytes(fileName);
                pullBudget = Math.Clamp(pullBudget, 64 * 1024, maxPull);

                bool adbOk = await _adbPreviewFetcher.TryFetchCappedAsync(
                    adb,
                    remotePath,
                    tempPath,
                    pullBudget,
                    knownFileSize > 0 ? knownFileSize : pullBudget,
                    cancellationToken);
                if (adbOk)
                {
                    if (!IsCoherentVideoPull(tempPath, isVideo, knownFileSize))
                    {
                        TryDeleteTemp(tempPath);
                        adbOk = false;
                    }

                    if (adbOk)
                    {
                        return (true, true);
                    }
                }

                TryDeleteTemp(tempPath);
            }

            bool ftpOk = await _fileDownloader.TryDownloadAsync(
                endpoint.Host,
                endpoint.Port,
                endpoint.User,
                endpoint.Pass,
                remotePath,
                tempPath,
                60,
                cancellationToken);

            if (!ftpOk && useFluentFtp)
            {
                FtpStreamingDownloader? fluent = FtpStreamingDownloader.GetOrCreate(endpoint, poolSize: 2, _logger);
                long cap = knownFileSize > 0 ? knownFileSize : 40L * 1024 * 1024;
                ftpOk = await fluent.TryDownloadCappedAsync(remotePath, tempPath, cap, cancellationToken);
            }

            return (ftpOk, false);
        }

        /// <summary>Reject truncated video pulls — Shell needs a coherent file.</summary>
        private static bool IsCoherentVideoPull(string tempPath, bool isVideo, long knownFileSize)
        {
            if (!isVideo || knownFileSize <= 0)
            {
                return true;
            }

            try
            {
                return new FileInfo(tempPath).Length >= knownFileSize;
            }
            catch
            {
                return false;
            }
        }
    }
}
