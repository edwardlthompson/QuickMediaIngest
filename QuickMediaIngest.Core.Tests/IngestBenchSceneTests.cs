#nullable enable
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.ImportUi;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class IngestBenchSceneTests
{
    [Fact]
    public void Complete_ShowsAfterglow_OpenEmptyIsFalse()
    {
        var model = new IngestBenchAppModel { DestinationRoot = "/tmp/QuickMediaIngest" };
        IngestBenchScene.Begin(model);
        Assert.True(model.IsImporting);
        Assert.Equal(IngestBenchActivity.Import, model.ActivityKind);
        IngestBenchScene.Progress(model, 150, "copy");
        Assert.Equal(100, model.ImportPercent);
        IngestBenchScene.Complete(model, IngestBenchCopy.Neutral, importedCount: 3);
        Assert.False(model.IsImporting);
        Assert.True(string.IsNullOrEmpty(model.ActivityKind));
        Assert.Equal("Import 3 succeeded, 0 failed.", model.ImportStatus);
        Assert.True(model.ShowAfterglow);
        Assert.Contains("QuickMediaIngest", model.AfterglowText);
        Assert.Single(model.Notifications);
        Assert.Equal(1, model.UnreadNotificationCount);
        Assert.Equal(model.AfterglowText, model.Notifications[0]);
        Assert.True(ImportProgressScene.ShowOpenDestination(false, model.AfterglowFolder));
        model.AfterglowFolder = string.Empty;
        Assert.False(IngestBenchScene.OpenFolder(model));
        IngestBenchScene.Dismiss(model);
        Assert.False(model.ShowAfterglow);
    }

    [Fact]
    public void Complete_WithFailures_SetsWarningCounts()
    {
        var model = new IngestBenchAppModel { DestinationRoot = "/tmp/QuickMediaIngest" };
        IngestBenchScene.Complete(model, IngestBenchCopy.Neutral, importedCount: 2, failedCount: 1);
        Assert.Equal("Import 2 succeeded, 1 failed.", model.ImportStatus);
        Assert.Contains("1", model.AfterglowText);
    }
}
