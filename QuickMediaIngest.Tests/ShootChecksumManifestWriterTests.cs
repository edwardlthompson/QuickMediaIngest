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

                string preDir = Path.Combine(Path.GetTempPath(), $"manifest-pre-{Guid.NewGuid():N}");
                Directory.CreateDirectory(preDir);
                try
                {
                    string prePath = await ShootChecksumManifestWriter.WritePrecomputedAsync(
                        preDir,
                        new[] { (Path.Combine(preDir, "a.jpg"), "abc123") });
                    Assert.NotNull(prePath);
                    string pre = await File.ReadAllTextAsync(prePath);
                    Assert.Contains("abc123 *a.jpg", pre);
                }
                finally
                {
                    Directory.Delete(preDir, recursive: true);
                }
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
