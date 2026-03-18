using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace QuickMediaIngest.Core
{
    public class LocalFileProvider : IFileProvider
    {
        public async Task CopyAsync(string srcPath, string destPath, CancellationToken token)
        {
            using (var sourceStream = new FileStream(srcPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            using (var destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                await sourceStream.CopyToAsync(destStream, 81920, token); 
            }
        }
    }
}
