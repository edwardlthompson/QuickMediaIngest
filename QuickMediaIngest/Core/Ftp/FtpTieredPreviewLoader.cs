#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Core
{
    /// <summary>Tiered FTP preview fetch: escalate byte budget only when decode fails.</summary>
    internal sealed class FtpTieredPreviewLoader
    {
        private readonly FtpFileDownloader _fileDownloader;
        private readonly IThumbnailService _thumbnailService;
        private readonly ILogger _logger;

        public FtpTieredPreviewLoader(
            FtpFileDownloader fileDownloader,
            IThumbnailService thumbnailService,
            ILogger logger)
        {
            _fileDownloader = fileDownloader;
            _thumbnailService = thumbnailService;
            _logger = logger;
        }

        public async Task<(bool Downloaded, long BytesUsed, int TierIndex)> TryDownloadBestTierAsync(
            FtpEndpoint endpoint,
            string remotePath,
            string fileName,
            string tempPath,
            bool useFluentFtp,
            int previewTimeoutSeconds,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<long> tiers = FtpPreviewDownloadLimits.GetPreviewByteTiers(fileName);
            FtpStreamingDownloader? fluent = useFluentFtp
                ? FtpStreamingDownloader.GetOrCreate(endpoint, poolSize: 3, _logger)
                : null;

            for (int tierIndex = 0; tierIndex < tiers.Count; tierIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                long maxBytes = tiers[tierIndex];
                TryDeleteTemp(tempPath);

                bool downloaded = await DownloadTierAsync(
                    endpoint,
                    fluent,
                    remotePath,
                    tempPath,
                    maxBytes,
                    previewTimeoutSeconds,
                    cancellationToken);

                if (!downloaded)
                {
                    continue;
                }

                DecodedThumbnail? thumb = TryDecodeDownloaded(fileName, tempPath, hints: null, FtpPreviewDecodeMode.TieredPartial);
                if (thumb != null)
                {
                    _logger.LogDebug(
                        "FTP preview decoded at tier {TierIndex} ({MaxBytes} bytes) for {RemotePath}.",
                        tierIndex,
                        maxBytes,
                        remotePath);
                    return (true, maxBytes, tierIndex);
                }
            }

            return (false, 0, -1);
        }

        public async Task<DecodedThumbnail?> TryLoadPreviewAsync(
            FtpEndpoint endpoint,
            string remotePath,
            string fileName,
            string tempPath,
            ThumbnailHints? hints,
            bool useFluentFtp,
            int previewTimeoutSeconds,
            CancellationToken cancellationToken)
        {
            (bool downloaded, _, _) = await TryDownloadBestTierAsync(
                endpoint,
                remotePath,
                fileName,
                tempPath,
                useFluentFtp,
                previewTimeoutSeconds,
                cancellationToken);

            if (!downloaded)
            {
                return null;
            }

            return TryDecodeDownloaded(fileName, tempPath, hints, FtpPreviewDecodeMode.TieredPartial);
        }

        public DecodedThumbnail? TryDecodeDownloaded(
            string fileName,
            string tempPath,
            ThumbnailHints? hints,
            FtpPreviewDecodeMode mode = FtpPreviewDecodeMode.TieredPartial) =>
            FtpTieredPreviewDecoder.TryDecodeDownloaded(
                fileName,
                tempPath,
                hints,
                _thumbnailService,
                _logger,
                mode);

        private async Task<bool> DownloadTierAsync(
            FtpEndpoint endpoint,
            FtpStreamingDownloader? fluent,
            string remotePath,
            string tempPath,
            long maxBytes,
            int previewTimeoutSeconds,
            CancellationToken cancellationToken)
        {
            if (fluent != null)
            {
                bool ok = await fluent.TryDownloadCappedAsync(remotePath, tempPath, maxBytes, cancellationToken);
                if (ok)
                {
                    return true;
                }
            }

            return await _fileDownloader.TryDownloadCappedAsync(
                endpoint.Host,
                endpoint.Port,
                endpoint.User,
                endpoint.Pass,
                remotePath,
                tempPath,
                maxBytes,
                previewTimeoutSeconds,
                cancellationToken);
        }

        private static void TryDeleteTemp(string tempPath)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
                // Ignore temp cleanup failures.
            }
        }
    }
}
