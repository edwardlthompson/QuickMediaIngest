#nullable enable
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Core
{
    /// <summary>FluentFTP streaming downloader with per-host connection pool for Max/Ultra modes.</summary>
    internal sealed class FtpStreamingDownloader : IAsyncDisposable
    {
        private static readonly ConcurrentDictionary<string, Lazy<FtpStreamingDownloader>> Pools = new(StringComparer.OrdinalIgnoreCase);

        private readonly FtpEndpoint _endpoint;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _slotLock;
        private readonly AsyncFtpClient[] _clients;
        private int _roundRobin;

        private FtpStreamingDownloader(FtpEndpoint endpoint, int poolSize, ILogger logger)
        {
            _endpoint = endpoint;
            _logger = logger;
            _slotLock = new SemaphoreSlim(poolSize, poolSize);
            _clients = new AsyncFtpClient[poolSize];
        }

        public static FtpStreamingDownloader GetOrCreate(FtpEndpoint endpoint, int poolSize, ILogger logger)
        {
            string key = $"{FtpHostNormalizer.Normalize(endpoint.Host)}:{endpoint.Port}:{endpoint.User}";
            return Pools.GetOrAdd(
                key,
                _ => new Lazy<FtpStreamingDownloader>(() => new FtpStreamingDownloader(endpoint, poolSize, logger))).Value;
        }

        public async Task<bool> TryDownloadCappedAsync(
            string remotePath,
            string localPath,
            long maxBytes,
            CancellationToken cancellationToken)
        {
            await _slotLock.WaitAsync(cancellationToken);
            int slot = Interlocked.Increment(ref _roundRobin) % _clients.Length;
            try
            {
                AsyncFtpClient client = await GetClientAsync(slot, cancellationToken);
                string normalized = FtpListingParser.NormalizeRemotePath(remotePath);
                await using var remoteStream = await client.OpenRead(normalized, token: cancellationToken);
                await using var localStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None);

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
                        int allowed = (int)Math.Min(read, maxBytes - totalBytes);
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
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogDebug(ex, "FluentFTP capped download failed for {RemotePath}.", remotePath);
                return false;
            }
            finally
            {
                _slotLock.Release();
            }
        }

        private async Task<AsyncFtpClient> GetClientAsync(int slot, CancellationToken cancellationToken)
        {
            AsyncFtpClient? client = _clients[slot];
            if (client != null && client.IsConnected)
            {
                return client;
            }

            client?.Dispose();
            client = new AsyncFtpClient(
                FtpHostNormalizer.Normalize(_endpoint.Host),
                _endpoint.User,
                _endpoint.Pass,
                _endpoint.Port)
            {
                Config =
                {
                    ConnectTimeout = 30000,
                    ReadTimeout = 30000,
                    DataConnectionConnectTimeout = 30000,
                    DataConnectionReadTimeout = 30000,
                    DataConnectionType = FtpDataConnectionType.AutoPassive,
                    EncryptionMode = FtpEncryptionMode.None,
                    DataConnectionEncryption = false,
                    SocketKeepAlive = true,
                    RetryAttempts = 2
                }
            };

            await client.Connect(cancellationToken);
            _clients[slot] = client;
            return client;
        }

        public async ValueTask DisposeAsync()
        {
            foreach (AsyncFtpClient? client in _clients)
            {
                if (client == null)
                {
                    continue;
                }

                try
                {
                    if (client.IsConnected)
                    {
                        await client.Disconnect();
                    }

                    client.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error disposing FluentFTP client.");
                }
            }

            _slotLock.Dispose();
        }
    }
}
