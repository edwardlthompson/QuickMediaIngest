#nullable enable
using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class AppPathsTests
{
    [Fact]
    public void DefaultAppPaths_EndsWithProductFolder()
    {
        string root = DefaultAppPaths.Instance.AppDataRoot;
        Assert.EndsWith("QuickMediaIngest", root);
        Assert.False(string.IsNullOrWhiteSpace(root));
    }
}
