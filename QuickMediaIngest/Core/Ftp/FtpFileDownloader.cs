#nullable enable
using System;
using System.IO;
using System.Net;
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
            CancellationToken cancellationToken)
        {
            Uri uri = FtpUriBuilder.Build(host, port, remotePath);
            int timeoutMs = Math.Max(5, timeoutSeconds) * 1000;

#pragma warning disable SYSLIB0014
            var request = (FtpWebRequest)WebRequest.Create(uri);
#pragma warning restore SYSLIB0014
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential(user, pass);
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = false;
            request.Timeout = timeoutMs;
            request.ReadWriteTimeout = timeoutMs;

            using var response = (FtpWebResponse)request.GetResponse();
            using var source = response.GetResponseStream();
            if (source == null)
            {
                return false;
            }

            using var dest = File.Create(localPath);
            byte[] buffer = new byte[65536];
            long totalBytes = 0;

            while (true)
            {
                int read = source.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (totalBytes + read > maxBytes)
                {
                    int allowed = (int)Math.Min(read, maxBytes - totalBytes);
                    if (allowed > 0)
                    {
                        dest.Write(buffer, 0, allowed);
                        totalBytes += allowed;
                    }

                    break;
                }

                dest.Write(buffer, 0, read);
                totalBytes += read;
            }

            return totalBytes > 0;
        }
    }
}
