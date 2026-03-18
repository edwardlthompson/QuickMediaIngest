using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace QuickMediaIngest.Core
{
    public class FtpFileProvider : IFileProvider
    {
        private readonly string _host;
        private readonly int _port;

        public FtpFileProvider(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public async Task CopyAsync(string srcPath, string destPath, CancellationToken token)
        {
            using (var client = new AsyncFtpClient(_host, "anonymous", "anonymous", _port))
            {
                await client.Connect(token);
                
                // Download file directly via FluentFTP core
                await client.DownloadFile(destPath, srcPath, FtpLocalExists.Overwrite, FtpVerify.None, null, token);
            }
        }
    }
}
