#nullable enable
using System.Threading;
using System.Threading.Tasks;

namespace QuickMediaIngest.Core.Services
{
    public sealed class FtpWorkflowService : IFtpWorkflowService
    {
        private readonly IFtpScanner _ftpScanner;

        public FtpWorkflowService(IFtpScanner ftpScanner)
        {
            _ftpScanner = ftpScanner;
        }

        public Task<(bool Success, string Message)> TestConnectionAsync(
            string host,
            int port,
            string user,
            string pass,
            string remotePath,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            return _ftpScanner.TestConnectionAsync(host, port, user, pass, remotePath, timeoutSeconds, cancellationToken);
        }
    }
}
