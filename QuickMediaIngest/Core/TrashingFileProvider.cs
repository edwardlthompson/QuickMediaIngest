#nullable enable
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace QuickMediaIngest.Core
{
    /// <summary>Local copies via inner provider; deletes go through <see cref="ITrashService"/> (gio trash on Linux).</summary>
    public sealed class TrashingFileProvider : IFileProvider
    {
        private readonly IFileProvider _inner;
        private readonly ITrashService _trash;

        public TrashingFileProvider(IFileProvider inner, ITrashService trash)
        {
            _inner = inner;
            _trash = trash;
        }

        public Task CopyAsync(
            string srcPath,
            string destPath,
            CancellationToken token,
            IProgress<long>? bytesCopied = null,
            long expectedBytes = 0) =>
            _inner.CopyAsync(srcPath, destPath, token, bytesCopied, expectedBytes);

        public Task DeleteAsync(string srcPath, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (_trash.TryTrash(srcPath))
            {
                return Task.CompletedTask;
            }

            if (File.Exists(srcPath))
            {
                File.Delete(srcPath);
            }

            return Task.CompletedTask;
        }
    }
}
