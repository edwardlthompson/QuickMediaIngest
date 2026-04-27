#nullable enable
using System.Threading;
using System.Threading.Tasks;

namespace QuickMediaIngest.Core.Services
{
    /// <summary>FTP probe operations used by the shell view model (keep FTP I/O off the UI thread).</summary>
    public interface IFtpWorkflowService
    {
        Task<(bool Success, string Message)> TestConnectionAsync(
            string host,
            int port,
            string user,
            string pass,
            string remotePath,
            int timeoutSeconds,
            CancellationToken cancellationToken);
    }
}
