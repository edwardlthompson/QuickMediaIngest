#nullable enable
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace QuickMediaIngest.Core
{
    public interface IFileProvider
    {
        /// <summary>
        /// Highly efficient copy or stream transfer from source location to local destination.
        /// </summary>
        Task CopyAsync(string srcPath, string destPath, CancellationToken token);

        /// <summary>
        /// Delete the source file from its origin (local disk or remote FTP).
        /// </summary>
        Task DeleteAsync(string srcPath, CancellationToken token);
    }
}
