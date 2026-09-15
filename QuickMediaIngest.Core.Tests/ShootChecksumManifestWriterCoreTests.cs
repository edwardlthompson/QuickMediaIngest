#nullable enable
using System.IO;
using System.Threading.Tasks;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class ShootChecksumManifestWriterCoreTests
{
    [Fact]
    public async Task WritePrecomputedAsync_WritesHashAndName()
    {
        string dir = Path.Combine(Path.GetTempPath(), "qmi-pre-" + Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            string? path = await ShootChecksumManifestWriter.WritePrecomputedAsync(
                dir,
                new[] { (Path.Combine(dir, "a.jpg"), "abc123") });
            Assert.NotNull(path);
            Assert.Contains("abc123 *a.jpg", await File.ReadAllTextAsync(path));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
