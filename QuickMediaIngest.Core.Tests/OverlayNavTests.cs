#nullable enable
using System.Linq;
using QuickMediaIngest.Core.Nav;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class OverlayNavTests
{
    [Fact]
    public void Pop_AtHome_IsNoOp()
    {
        var nav = new OverlayNav();
        Assert.Equal(OverlayId.Home, nav.Peek);
        Assert.False(nav.CanPop);
        Assert.Equal(OverlayId.Home, nav.Pop());
        Assert.Equal(OverlayId.Home, nav.Peek);
    }

    [Fact]
    public void Settings_ThenAppInfo_ThenFeedback_PopsToAboutThenHome()
    {
        var nav = new OverlayNav();
        nav.Push(OverlayId.Settings);
        nav.OpenAboutFromSettings();
        nav.Push(OverlayId.Feedback);

        Assert.Equal(new[] { OverlayId.About, OverlayId.Feedback }, nav.Stack.ToArray());
        Assert.Equal(OverlayId.Feedback, nav.Peek);

        Assert.Equal(OverlayId.Feedback, nav.Pop());
        Assert.True(nav.IsVisible(OverlayId.About));
        Assert.Equal(OverlayId.About, nav.Pop());
        Assert.Equal(OverlayId.Home, nav.Peek);
        Assert.False(nav.CanPop);
    }

    [Fact]
    public void Serialize_RoundTrips_Stack()
    {
        var nav = new OverlayNav();
        nav.Push(OverlayId.About);
        nav.Push(OverlayId.Feedback);
        OverlayNav copy = OverlayNav.Deserialize(nav.Serialize());
        Assert.Equal(nav.Serialize(), copy.Serialize());
        Assert.Equal(OverlayId.Feedback, copy.Peek);
    }

    [Fact]
    public void History_AndScanExclusions_AreDistinctOverlays()
    {
        var nav = new OverlayNav();
        nav.Push(OverlayId.History);
        Assert.True(nav.IsVisible(OverlayId.History));
        nav.Push(OverlayId.ScanExclusions);
        Assert.True(nav.IsVisible(OverlayId.ScanExclusions));
        Assert.False(nav.IsVisible(OverlayId.History));
        nav.Pop();
        Assert.True(nav.IsVisible(OverlayId.History));
    }
}
