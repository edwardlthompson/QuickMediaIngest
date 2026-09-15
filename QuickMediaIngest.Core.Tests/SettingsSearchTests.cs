#nullable enable
using QuickMediaIngest.Core.Settings;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class SettingsSearchTests
{
    [Fact]
    public void BlankQuery_MatchesEverything()
    {
        Assert.True(SettingsSearch.IsBlank(null));
        Assert.True(SettingsSearch.IsBlank("  "));
        Assert.True(SettingsSearch.Matches("", "Theme", "Destination"));
    }

    [Fact]
    public void Query_FiltersCaseInsensitive()
    {
        Assert.True(SettingsSearch.Matches("theme", "Appearance", "Theme mode"));
        Assert.False(SettingsSearch.Matches("ftp", "Appearance", "Theme mode"));
    }

    [Fact]
    public void AnyMatch_DetectsEmptyResults()
    {
        string[][] sections = { new[] { "Theme" }, new[] { "Naming" } };
        Assert.True(SettingsSearch.AnyMatch("nam", sections));
        Assert.False(SettingsSearch.AnyMatch("xyz", sections));
    }
}
