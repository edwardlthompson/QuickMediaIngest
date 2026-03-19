using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace QuickMediaIngest.Core
{
    public class FtpFileProvider : IFileProvider, IAsyncDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _user;
        private readonly string _pass;
    private AsyncFtpClient? _client;
    private readonly SemaphoreSlim _clientLock = new SemaphoreSlim(1, 1);

        public FtpFileProvider(string host, int port, string user, string pass)
        {
            _host = host;
            _port = port;
            _user = user;
            _pass = pass;
        }

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
                    await _client.Connect(token);
                }

                await _client.DownloadFile(destPath, srcPath, FtpLocalExists.Overwrite, FtpVerify.None, null, token);
            }
            finally
            {
                _clientLock.Release();
            }
        }

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
                    await _client.Connect(token);
                }

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
