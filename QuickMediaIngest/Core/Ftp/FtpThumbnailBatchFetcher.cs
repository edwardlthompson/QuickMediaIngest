#nullable enable
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Core
{
    /// <summary>Pooled FluentFTP session for capped preview downloads during a thumbnail batch.</summary>
    internal sealed class FtpThumbnailBatchFetcher : IAsyncDisposable
    {
        private readonly FtpEndpoint _endpoint;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _clientLock = new(1, 1);
        private AsyncFtpClient? _client;

        public FtpThumbnailBatchFetcher(FtpEndpoint endpoint, ILogger logger)
        {
            _endpoint = endpoint;
            _logger = logger;
        }

        public static long GetMaxPreviewBytes(string fileName) =>
            FtpPreviewDownloadLimits.GetMaxPreviewBytes(fileName);

        public async Task<bool> TryDownloadPreviewAsync(
            string remotePath,
            string fileName,
            string localPath,
            CancellationToken cancellationToken)
        {
            await _clientLock.WaitAsync(cancellationToken);
            try
            {
                await EnsureConnectedAsync(cancellationToken);
                long maxBytes = GetMaxPreviewBytes(fileName);
                return await DownloadCappedAsync(remotePath, localPath, maxBytes, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogDebug(ex, "FluentFTP preview download failed for {RemotePath}.", remotePath);
                return false;
            }
            finally
            {
                _clientLock.Release();
            }
        }

        private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
        {
            if (_client != null && _client.IsConnected)
            {
                return;
            }

            _client?.Dispose();
            _client = new AsyncFtpClient(
                FtpHostNormalizer.Normalize(_endpoint.Host),
                _endpoint.User,
                _endpoint.Pass,
                _endpoint.Port);

            _client.Config.ConnectTimeout = 30000;
            _client.Config.ReadTimeout = 30000;
            _client.Config.DataConnectionConnectTimeout = 30000;
            _client.Config.DataConnectionReadTimeout = 30000;
            _client.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
            _client.Config.EncryptionMode = FtpEncryptionMode.None;
            _client.Config.DataConnectionEncryption = false;
            _client.Config.SocketKeepAlive = true;
            _client.Config.RetryAttempts = 2;

            await _client.Connect(cancellationToken);
        }

        private async Task<bool> DownloadCappedAsync(
            string remotePath,
            string localPath,
            long maxBytes,
            CancellationToken cancellationToken)
        {
            if (_client == null)
            {
                return false;
            }

            string normalized = FtpListingParser.NormalizeRemotePath(remotePath);
            await using var remoteStream = await _client.OpenRead(normalized, token: cancellationToken);
            await using var localStream = new FileStream(
                localPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);

            byte[] buffer = new byte[8192];
            long totalBytes = 0;

            while (true)
            {
                int read = await remoteStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read <= 0)
                {
                    break;
                }

                if (totalBytes + read > maxBytes)
                {
                    int allowed = (int)(maxBytes - totalBytes);
                    if (allowed > 0)
                    {
                        await localStream.WriteAsync(buffer.AsMemory(0, allowed), cancellationToken);
                        totalBytes += allowed;
                    }

                    break;
                }

                await localStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                totalBytes += read;
            }

            return totalBytes > 0;
        }

        public async ValueTask DisposeAsync()
        {
            await _clientLock.WaitAsync();
            try
            {
                if (_client != null)
                {
                    if (_client.IsConnected)
                    {
                        await _client.Disconnect();
                    }

                    _client.Dispose();
                    _client = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error disposing FTP thumbnail batch fetcher.");
            }
            finally
            {
                _clientLock.Release();
                _clientLock.Dispose();
            }
        }
    }
}
