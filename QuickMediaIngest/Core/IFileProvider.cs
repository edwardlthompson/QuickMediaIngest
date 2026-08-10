#nullable enable
using System;
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
        /// <param name="expectedBytes">
        /// Optional known file size (bytes). ADB uses this for a size-scaled pull timeout; other providers ignore it.
        /// </param>
        Task CopyAsync(
            string srcPath,
            string destPath,
            CancellationToken token,
            IProgress<long>? bytesCopied = null,
            long expectedBytes = 0);

        /// <summary>
        /// Delete the source file from its origin (local disk or remote FTP).
        /// </summary>
        Task DeleteAsync(string srcPath, CancellationToken token);
    }
}
