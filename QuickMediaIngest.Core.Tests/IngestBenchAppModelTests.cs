#nullable enable
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Models;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class IngestBenchAppModelTests
{
    [Fact]
    public void FirstRun_WaitingForCard_UntilSources()
    {
        var bench = new IngestBenchAppModel();
        Assert.True(bench.IsFirstRun);
        Assert.True(bench.ShowWaitingForCard);
        bench.NoteSources(1);
        Assert.False(bench.IsFirstRun);
        Assert.False(bench.ShowWaitingForCard);
    }

    [Fact]
    public void DismissOnboarding_ClearsFirstRun()
    {
        var bench = new IngestBenchAppModel();
        bench.DismissOnboarding();
        Assert.False(bench.IsFirstRun);
    }

    [Fact]
    public void DestinationChipLabel_UsesLeafFolder()
    {
        var bench = new IngestBenchAppModel { DestinationRoot = "/tmp/shoots/QuickMediaIngest" };
        Assert.Equal("QuickMediaIngest", bench.DestinationChipLabel);
        Assert.Equal("Saving to /tmp/shoots/QuickMediaIngest", bench.SaveLocationNote);
    }

    [Fact]
    public void SaveLocationNote_EmptyUntilDestinationChosen()
    {
        var bench = new IngestBenchAppModel();
        Assert.Equal("Choose a save location", bench.SaveLocationNote);
    }

    [Fact]
    public void CommandBarCompact_HidesPrimaryExtras()
    {
        var bench = new IngestBenchAppModel();
        Assert.True(bench.ShowPrimaryExtras);
        bench.CommandBarCompact = true;
        Assert.False(bench.ShowPrimaryExtras);
        bench.CommandBarCompact = false;
        Assert.True(bench.ShowPrimaryExtras);
    }

    [Fact]
    public void ShowEject_OnlyWhenSourcesExist()
    {
        var bench = new IngestBenchAppModel();
        Assert.False(bench.ShowEject);
        bench.NoteSources(1);
        Assert.True(bench.ShowEject);
    }

    [Fact]
    public void CanImport_RequiresSelectedShoot()
    {
        var bench = new IngestBenchAppModel();
        Assert.False(bench.CanImport);
        bench.SelectedShoot = new ItemGroup { Title = "Studio" };
        Assert.True(bench.CanImport);
        bench.IsImporting = true;
        Assert.False(bench.CanImport);
    }

    [Fact]
    public void ShowEmptyCard_HidesWhenFtpFailure()
    {
        var bench = new IngestBenchAppModel();
        Assert.True(bench.ShowWaitingForCard);
        bench.ShowFtpFailure = true;
        Assert.True(bench.ShowFtpFailure);
    }

    [Fact]
    public void ShowNotifyBadge_FollowsUnreadCount()
    {
        var bench = new IngestBenchAppModel();
        Assert.False(bench.ShowNotifyBadge);
        bench.UnreadNotificationCount = 2;
        Assert.True(bench.ShowNotifyBadge);
    }

    [Fact]
    public void ShowActivityProgress_FollowsActivityKind()
    {
        var bench = new IngestBenchAppModel();
        Assert.False(bench.ShowActivityProgress);
        bench.ActivityKind = IngestBenchActivity.Import;
        Assert.True(bench.ShowActivityProgress);
        bench.ActivityKind = string.Empty;
        Assert.False(bench.ShowActivityProgress);
    }
}
