#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Core
{
    internal sealed partial class FtpThumbnailPipeline
    {
        private async Task<DecodedThumbnail?> TryTieredDownloadAndDecodeAsync(
            FtpEndpoint endpoint,
            string remotePath,
            string fileName,
            string tempPath,
            ThumbnailHints? hints,
            bool useFluentFtp,
            SemaphoreSlim decodeGate,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<long> tiers = FtpPreviewDownloadLimits.GetPreviewByteTiers(fileName);
            FtpStreamingDownloader? fluent = useFluentFtp
                ? FtpStreamingDownloader.GetOrCreate(endpoint, poolSize: 3, _logger)
                : null;

            for (int tierIndex = 0; tierIndex < tiers.Count; tierIndex++)
            {
                long maxBytes = tiers[tierIndex];
                FtpPreviewDecodeMode decodeMode = tierIndex < tiers.Count - 1
                    ? FtpPreviewDecodeMode.TieredPartial
                    : FtpPreviewDecodeMode.TieredFinalCap;

                cancellationToken.ThrowIfCancellationRequested();
                TryDeleteTemp(tempPath);

                bool downloaded = fluent != null
                    ? await fluent.TryDownloadCappedAsync(remotePath, tempPath, maxBytes, cancellationToken)
                    : await _fileDownloader.TryDownloadCappedAsync(
                        endpoint.Host,
                        endpoint.Port,
                        endpoint.User,
                        endpoint.Pass,
                        remotePath,
                        tempPath,
                        maxBytes,
                        20,
                        cancellationToken);

                if (!downloaded && fluent != null)
                {
                    downloaded = await _fileDownloader.TryDownloadCappedAsync(
                        endpoint.Host,
                        endpoint.Port,
                        endpoint.User,
                        endpoint.Pass,
                        remotePath,
                        tempPath,
                        maxBytes,
                        20,
                        cancellationToken);
                }

                if (!downloaded)
                {
                    continue;
                }

                await decodeGate.WaitAsync(cancellationToken);
                try
                {
                    DecodedThumbnail? thumb = _tieredLoader.TryDecodeDownloaded(
                        fileName,
                        tempPath,
                        hints,
                        decodeMode);
                    if (thumb != null)
                    {
                        _logger.LogDebug(
                            "FTP preview decoded at {MaxBytes} bytes (tier {TierIndex}) for {RemotePath}.",
                            maxBytes,
                            tierIndex,
                            remotePath);
                        return thumb;
                    }
                }
                finally
                {
                    decodeGate.Release();
                }
            }

            return null;
        }
    }
}
