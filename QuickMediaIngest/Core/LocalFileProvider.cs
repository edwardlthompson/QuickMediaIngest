#nullable enable
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Provides file operations for local disk files.
    /// </summary>
    public class LocalFileProvider : IFileProvider
    {
        private readonly ILogger<LocalFileProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalFileProvider"/> class.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output.</param>
        public LocalFileProvider(ILogger<LocalFileProvider> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Copies a file from a local source to a local destination asynchronously.
        /// </summary>
        /// <param name="srcPath">Source file path.</param>
        /// <param name="destPath">Destination file path.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task CopyAsync(string srcPath, string destPath, CancellationToken token)
        {
            _logger.LogDebug("Copying local file from {SourcePath} to {DestinationPath}.", srcPath, destPath);
            const int bufferSize = 1024 * 1024;
            var options = FileOptions.Asynchronous | FileOptions.SequentialScan;
            await using (var sourceStream = new FileStream(srcPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, options))
            await using (var destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, options))
            {
                await sourceStream.CopyToAsync(destStream, bufferSize, token);
            }
        }

        /// <summary>
        /// Deletes a local file at the specified path.
        /// </summary>
        /// <param name="srcPath">Source file path.</param>
        /// <param name="token">Cancellation token.</param>
        public Task DeleteAsync(string srcPath, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (File.Exists(srcPath))
            {
                _logger.LogInformation("Deleting local source file {SourcePath}.", srcPath);
                File.Delete(srcPath);
            }

            return Task.CompletedTask;
        }
    }
}
