#nullable enable
using System.Globalization;
using QuickMediaIngest.Core.Chrome;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class UiReadingOrderTests
{
    [Theory]
    [InlineData("en", false)]
    [InlineData("fr", false)]
    [InlineData("es", false)]
    [InlineData("de", false)]
    [InlineData("ja", false)]
    [InlineData("ar", true)]
    [InlineData("he", true)]
    public void KnownTags_MatchTextInfo(string tag, bool rtl)
    {
        Assert.Equal(rtl, UiReadingOrder.IsRightToLeft(tag));
    }

    [Fact]
    public void EmptyTag_FollowsCurrentUiCulture()
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ar");
            Assert.True(UiReadingOrder.IsRightToLeft(""));
            Assert.True(UiReadingOrder.IsRightToLeft("system"));
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
            Assert.False(UiReadingOrder.IsRightToLeft(""));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
