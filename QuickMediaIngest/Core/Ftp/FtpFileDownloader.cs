#nullable enable
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core
{
    /// <summary>Reliable FTP download with retries (FtpWebRequest) — same stack as directory scan.</summary>
    public sealed class FtpFileDownloader
    {
        private readonly ILogger<FtpFileDownloader> _logger;

        public FtpFileDownloader(ILogger<FtpFileDownloader> logger)
        {
            _logger = logger;
        }

        public static Uri BuildFileUri(string host, int port, string remotePath) =>
            FtpUriBuilder.Build(host, port, remotePath);

        public async Task<bool> TryDownloadPreviewAsync(
            string host,
            int port,
            string user,
            string pass,
            string remotePath,
            string localPath,
            string fileName,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            long maxBytes = FtpPreviewDownloadLimits.GetMaxPreviewBytes(fileName);
            return await TryDownloadCappedAsync(
                host,
                port,
                user,
                pass,
                remotePath,
                localPath,
                maxBytes,
                timeoutSeconds,
                cancellationToken);
        }

        public async Task<bool> TryDownloadAsync(
            string host,
            int port,
            string user,
            string pass,
            string remotePath,
            string localPath,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            return await TryDownloadCappedAsync(
                host,
                port,
                user,
                pass,
                remotePath,
                localPath,
                maxBytes: long.MaxValue,
                timeoutSeconds,
                cancellationToken);
        }

        public async Task<bool> TryDownloadCappedAsync(
            string host,
            int port,
            string user,
            string pass,
            string remotePath,
            string localPath,
            long maxBytes,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(pass))
            {
                _logger.LogWarning("FTP download skipped for {RemotePath}: password is empty.", remotePath);
                return false;
            }

            Exception? lastError = null;

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    bool ok = await Task.Run(
                        () => DownloadCappedSync(host, port, user, pass, remotePath, localPath, maxBytes, timeoutSeconds, cancellationToken),
                        cancellationToken);

                    if (ok)
                    {
                        return true;
                    }

                    lastError = new InvalidOperationException($"FTP download produced no data for {remotePath}");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    lastError = ex;
                }

                TryDeletePartial(localPath);

                if (attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt), cancellationToken);
                }
            }

            _logger.LogWarning(lastError, "FTP download failed after retries for {RemotePath}.", remotePath);
            return false;
        }

        private void TryDeletePartial(string localPath)
        {
            try
            {
                if (File.Exists(localPath))
                {
                    File.Delete(localPath);
                }
            }
            catch
            {
                // Ignore cleanup failures.
            }
        }

        private static bool DownloadCappedSync(
            string host,
            int port,
            string user,
            string pass,
            string remotePath,
            string localPath,
            long maxBytes,
            int timeoutSeconds,
            CancellationToken cancellationToken) =>
            FtpDownloadSync.DownloadCapped(
                host,
                port,
                user,
                pass,
                remotePath,
                localPath,
                maxBytes,
                timeoutSeconds,
                cancellationToken);
    }
}
