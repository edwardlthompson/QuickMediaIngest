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
        private AdbTransferSession? _adbSession;
        private IAdbPreviewFetcher? _adbPreviewFetcher;
        private IAdbVideoThumbnailFetcher? _adbVideoThumbnailFetcher;

        private async Task<(DecodedThumbnail? Thumb, bool ViaAdb)> TryTieredDownloadAndDecodeAsync(
            FtpEndpoint endpoint,
            string remotePath,
            string fileName,
            long knownFileSize,
            string tempPath,
            ThumbnailHints? hints,
            bool useFluentFtp,
            SemaphoreSlim decodeGate,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<long> tiers = FtpPreviewDownloadLimits.GetFetchTiers(fileName, knownFileSize);

            if (_adbSession is { } adb && _adbPreviewFetcher != null)
            {
                for (int tierIndex = 0; tierIndex < tiers.Count; tierIndex++)
                {
                    long maxBytes = tiers[tierIndex];

                    cancellationToken.ThrowIfCancellationRequested();
                    TryDeleteTemp(tempPath);

                    bool downloaded = await _adbPreviewFetcher.TryFetchCappedAsync(
                        adb,
                        remotePath,
                        tempPath,
                        maxBytes,
                        knownFileSize,
                        cancellationToken);

                    if (!downloaded)
                    {
                        continue;
                    }

                    await decodeGate.WaitAsync(cancellationToken);
                    try
                    {
                        (FtpPreviewDecodeMode decodeMode, ThumbnailHints decodeHints) =
                            ResolveDecodeHints(tempPath, knownFileSize, maxBytes, tierIndex, tiers.Count, hints);
                        DecodedThumbnail? thumb = _tieredLoader.TryDecodeDownloaded(
                            fileName,
                            tempPath,
                            decodeHints,
                            decodeMode);
                        if (thumb != null)
                        {
                            _logger.LogDebug(
                                "ADB preview decoded at {MaxBytes} bytes (tier {TierIndex}) for {RemotePath}.",
                                maxBytes,
                                tierIndex,
                                remotePath);
                            return (thumb, true);
                        }
                    }
                    finally
                    {
                        decodeGate.Release();
                    }
                }
            }

            FtpStreamingDownloader? fluent = useFluentFtp
                ? FtpStreamingDownloader.GetOrCreate(endpoint, poolSize: 3, _logger)
                : null;

            for (int tierIndex = 0; tierIndex < tiers.Count; tierIndex++)
            {
                long maxBytes = tiers[tierIndex];

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
                    (FtpPreviewDecodeMode decodeMode, ThumbnailHints decodeHints) =
                        ResolveDecodeHints(tempPath, knownFileSize, maxBytes, tierIndex, tiers.Count, hints);
                    DecodedThumbnail? thumb = _tieredLoader.TryDecodeDownloaded(
                        fileName,
                        tempPath,
                        decodeHints,
                        decodeMode);
                    if (thumb != null)
                    {
                        _logger.LogDebug(
                            "FTP preview decoded at {MaxBytes} bytes (tier {TierIndex}) for {RemotePath}.",
                            maxBytes,
                            tierIndex,
                            remotePath);
                        return (thumb, false);
                    }
                }
                finally
                {
                    decodeGate.Release();
                }
            }

            return (null, false);
        }
    }
}
