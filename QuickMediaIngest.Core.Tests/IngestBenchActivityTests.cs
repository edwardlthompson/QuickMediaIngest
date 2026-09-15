#nullable enable
using System.IO;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Prompt;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class IngestBenchActivityTests
{
    [Fact]
    public void Begin_ShowsMeter_IdleHides()
    {
        var model = new IngestBenchAppModel();
        Assert.False(model.ShowActivityProgress);
        IngestBenchActivity.Begin(model, IngestBenchActivity.Scan, "Scanning…");
        Assert.True(model.ShowActivityProgress);
        Assert.True(model.ActivityIndeterminate);
        Assert.Equal(0, model.ImportPercent);
        IngestBenchActivity.Report(model, 40, "Scanning DCIM…");
        Assert.False(model.ActivityIndeterminate);
        Assert.Equal(40, model.ImportPercent);
        IngestBenchActivity.Idle(model);
        Assert.False(model.ShowActivityProgress);
        Assert.Equal("Scanning DCIM…", model.ImportStatus);
    }

    [Fact]
    public void HostBegin_PostsToDispatcher()
    {
        var paths = new ActivityTempPaths();
        using IngestBenchHost host = IngestBenchHost.Create(
            InlineUiDispatcher.Instance,
            paths,
            SilentUserPrompt.Instance);
        IngestBenchActivity.Begin(host, IngestBenchActivity.Thumbs, "Building thumbnails…");
        Assert.Equal(IngestBenchActivity.Thumbs, host.Model.ActivityKind);
        IngestBenchActivity.Idle(host);
        Assert.False(host.Model.ShowActivityProgress);
    }

    [Fact]
    public void Counts_IncludesSucceededAndFailed()
    {
        Assert.Equal("Thumbnails 8 succeeded, 2 failed.", IngestBenchActivity.Counts("Thumbnails", 8, 2));
        Assert.Equal("Import 4 succeeded, 0 failed.", IngestBenchActivity.Counts("Import", 4, 0));
    }
}

file sealed class ActivityTempPaths : IAppPaths
{
    public ActivityTempPaths()
    {
        AppDataRoot = Path.Combine(Path.GetTempPath(), "qmi-activity-" + Path.GetRandomFileName());
        Directory.CreateDirectory(AppDataRoot);
    }

    public string AppDataRoot { get; }
}
