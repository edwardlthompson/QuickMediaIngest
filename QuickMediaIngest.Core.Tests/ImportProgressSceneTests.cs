#nullable enable
using QuickMediaIngest.Core.ImportUi;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class ImportProgressSceneTests
{
    [Fact]
    public void ClampPercent_Bounds()
    {
        Assert.Equal(0, ImportProgressScene.ClampPercent(-4));
        Assert.Equal(100, ImportProgressScene.ClampPercent(140));
        Assert.Equal(42, ImportProgressScene.ClampPercent(42));
    }

    [Fact]
    public void OpenDestination_OnlyWhenIdleWithPath()
    {
        Assert.False(ImportProgressScene.ShowOpenDestination(true, @"C:\Pictures"));
        Assert.False(ImportProgressScene.ShowOpenDestination(false, " "));
        Assert.True(ImportProgressScene.ShowOpenDestination(false, @"C:\Pictures"));
    }

    [Fact]
    public void DimList_NeverDims()
    {
        Assert.False(ImportProgressScene.DimList(true));
        Assert.False(ImportProgressScene.DimList(false));
    }
}
