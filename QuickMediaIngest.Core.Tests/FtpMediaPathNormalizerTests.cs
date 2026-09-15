using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests;

public class FtpMediaPathNormalizerCoreTests
{
    [Fact]
    public void GetRenderedSiblingRemotePaths_IncludesAvifJxlHif()
    {
        string[] paths = FtpMediaPathNormalizer
            .GetRenderedSiblingRemotePaths("/DCIM/a.dng", "a.dng")
            .ToArray();
        Assert.Equal(".heic", Path.GetExtension(paths[0]));
        Assert.Equal(".heif", Path.GetExtension(paths[1]));
        Assert.Contains(paths, p => p.EndsWith(".avif", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(paths, p => p.EndsWith(".jxl", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(paths, p => p.EndsWith(".hif", StringComparison.OrdinalIgnoreCase));
    }
}
