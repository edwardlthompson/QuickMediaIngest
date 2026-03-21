#nullable enable
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Provides file operations over FTP using FluentFTP.
    /// </summary>
    public class FtpFileProvider : IFileProvider, IAsyncDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _user;
        private readonly string _pass;
    private AsyncFtpClient? _client;
    private readonly SemaphoreSlim _clientLock = new SemaphoreSlim(1, 1);
        private readonly ILogger<FtpFileProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpFileProvider"/> class.
        /// </summary>
        /// <param name="host">FTP server host.</param>
        /// <param name="port">FTP server port.</param>
        /// <param name="user">FTP username.</param>
        /// <param name="pass">FTP password.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        public FtpFileProvider(string host, int port, string user, string pass, ILogger<FtpFileProvider> logger)
        {
            _host = host;
            _port = port;
            _user = user;
            _pass = pass;
            _logger = logger;
        }

        /// <summary>
        /// Batch lists files in a directory using FluentFTP for performance.
        /// </summary>
        /// <param name="remotePath">Remote FTP directory path.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of file paths.</returns>
        public async Task<List<string>> ListFilesAsync(string remotePath, CancellationToken token)
        {
            await _clientLock.WaitAsync(token);
            try
            {
                if (_client == null)
                {
                    _client = new AsyncFtpClient(_host, _user, _pass, _port);
                    _client.Config.ConnectTimeout = 30000;
                    _client.Config.ReadTimeout = 30000;
                    _client.Config.DataConnectionConnectTimeout = 30000;
                    _client.Config.DataConnectionReadTimeout = 30000;
                    _client.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
                    _client.Config.EncryptionMode = FtpEncryptionMode.None;
                    _client.Config.DataConnectionEncryption = false;
                    _client.Config.SocketKeepAlive = true;
                    _client.Config.RetryAttempts = 2;
                    await _client.Connect(token);
                    _logger.LogInformation("Connected FTP file provider to {Host}:{Port} for batch listing.", _host, _port);
                }

                var items = new List<string>();
                foreach (var item in await _client.GetListing(remotePath, FtpListOption.Recursive, token))
                {
                    if (item.Type == FtpObjectType.File)
                    {
                        items.Add(item.FullName);
                    }
                }
                return items;
            }
            finally
            {
                _clientLock.Release();
            }
        }

        /// <summary>
        /// Copies a file from the FTP server to a local destination.
        /// </summary>
        /// <param name="srcPath">Source path on the FTP server.</param>
        /// <param name="destPath">Destination local path.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task CopyAsync(string srcPath, string destPath, CancellationToken token)
        {
            await _clientLock.WaitAsync(token);
            try
            {
                if (_client == null)
                {
                    _client = new AsyncFtpClient(_host, _user, _pass, _port);
                    _client.Config.ConnectTimeout = 30000;
                    _client.Config.ReadTimeout = 30000;
                    _client.Config.DataConnectionConnectTimeout = 30000;
                    _client.Config.DataConnectionReadTimeout = 30000;
                    _client.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
                    _client.Config.EncryptionMode = FtpEncryptionMode.None;
                    _client.Config.DataConnectionEncryption = false;
                    _client.Config.SocketKeepAlive = true;
                    _client.Config.RetryAttempts = 2;
                    await _client.Connect(token);
                    _logger.LogInformation("Connected FTP file provider to {Host}:{Port}.", _host, _port);
                }

                _logger.LogInformation("Downloading FTP file {SourcePath} to {DestinationPath}.", srcPath, destPath);
                await _client.DownloadFile(destPath, srcPath, FtpLocalExists.Overwrite, FtpVerify.None, null, token);
            }
            finally
            {
                _clientLock.Release();
            }
        }

        /// <summary>
        /// Deletes a file from the FTP server.
        /// </summary>
        /// <param name="srcPath">Source path on the FTP server.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task DeleteAsync(string srcPath, CancellationToken token)
        {
            await _clientLock.WaitAsync(token);
            try
            {
                if (_client == null)
                {
                    _client = new AsyncFtpClient(_host, _user, _pass, _port);
                    _client.Config.ConnectTimeout = 30000;
                    _client.Config.ReadTimeout = 30000;
                    _client.Config.DataConnectionConnectTimeout = 30000;
                    _client.Config.DataConnectionReadTimeout = 30000;
                    _client.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
                    _client.Config.EncryptionMode = FtpEncryptionMode.None;
                    _client.Config.DataConnectionEncryption = false;
                    _client.Config.SocketKeepAlive = true;
                    _client.Config.RetryAttempts = 2;
                    await _client.Connect(token);
                    _logger.LogInformation("Connected FTP file provider to {Host}:{Port} for delete operation.", _host, _port);
                }

                _logger.LogInformation("Deleting FTP source file {SourcePath}.", srcPath);
                await _client.DeleteFile(srcPath, token);
            }
            finally
            {
                _clientLock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _clientLock.WaitAsync();
            try
            {
                if (_client != null)
                {
                    try
                    {
                        if (_client.IsConnected)
                        {
                            await _client.Disconnect();
                            _logger.LogInformation("Disconnected FTP file provider from {Host}:{Port}.", _host, _port);
                        }
                    }
                    catch
                    {
                        // Ignore disconnect errors.
                    }

                    _client.Dispose();
                    _client = null;
                }
            }
            finally
            {
                _clientLock.Release();
                _clientLock.Dispose();
            }
        }
    }
}
