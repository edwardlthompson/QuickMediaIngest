#nullable enable
using QuickMediaIngest.Core.AppModel;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class PrefsLayoutTests
{
    [Fact]
    public void ClampPane_RejectsEmptyAndCapsWidth()
    {
        Assert.Equal(PrefsLayout.DefaultPane, PrefsLayout.ClampPane(0));
        Assert.Equal(PrefsLayout.DefaultPane, PrefsLayout.ClampPane(double.NaN));
        Assert.Equal(PrefsLayout.MinPane, PrefsLayout.ClampPane(10));
        Assert.Equal(PrefsLayout.MaxPane, PrefsLayout.ClampPane(9000));
        Assert.Equal(500, PrefsLayout.ClampPane(500));
        Assert.Equal(480, PrefsLayout.CoalescePane(480, 0));
        Assert.Equal(640, PrefsLayout.CoalescePane(480, 640));
    }

    [Fact]
    public void HasSavedSize_IgnoresUnsetOrigin()
    {
        Assert.False(PrefsLayout.HasSavedSize(0, 0));
        Assert.False(PrefsLayout.HasSavedSize(200, 720));
        Assert.True(PrefsLayout.HasSavedSize(1100, 720));
    }

    [Fact]
    public void FormatParse_RoundTripsWindowAndPane()
    {
        var dto = new PrefsStore.FileDto
        {
            WindowWidth = 1600,
            WindowHeight = 900,
            WindowLeft = 40,
            WindowTop = 50,
            WindowMaximized = true,
            WindowPositionSet = true,
            PreviewPaneWidth = 420,
        };
        PrefsStore.FileDto again = PrefsStore.Parse(PrefsStore.Format(dto));
        Assert.Equal(1600, again.WindowWidth);
        Assert.Equal(900, again.WindowHeight);
        Assert.Equal(40, again.WindowLeft);
        Assert.Equal(50, again.WindowTop);
        Assert.True(again.WindowMaximized);
        Assert.True(again.WindowPositionSet);
        Assert.Equal(420, again.PreviewPaneWidth);
    }
}
