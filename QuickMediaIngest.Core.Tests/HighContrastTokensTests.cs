#nullable enable
using QuickMediaIngest.Core.Chrome;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class HighContrastTokensTests
{
    [Fact]
    public void HighContrast_SkipsBlur()
    {
        Assert.True(HighContrastTokens.ShouldApply(true));
        Assert.False(HighContrastTokens.ShouldApply(false));
        Assert.Equal(0d, HighContrastTokens.OverlayBlurRadius(true));
        Assert.Equal(8d, HighContrastTokens.OverlayBlurRadius(false));
    }
}
