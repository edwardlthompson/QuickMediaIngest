#nullable enable
using System;
using System.IO;
using System.Threading.Tasks;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class ShootChecksumManifestWriterTests
    {
        [Fact]
        public async Task WriteManifestAsync_GeneratesValidSha256Manifest()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"manifest-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                string file1 = Path.Combine(tempDir, "photo1.jpg");
                string file2 = Path.Combine(tempDir, "photo2.jpg");
                await File.WriteAllTextAsync(file1, "sample-content-1");
                await File.WriteAllTextAsync(file2, "sample-content-2");

                string? manifestPath = await ShootChecksumManifestWriter.WriteManifestAsync(tempDir);
                Assert.NotNull(manifestPath);
                Assert.True(File.Exists(manifestPath));

                string content = await File.ReadAllTextAsync(manifestPath);
                Assert.Contains("photo1.jpg", content);
                Assert.Contains("photo2.jpg", content);
                Assert.Contains("*photo1.jpg", content);
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
