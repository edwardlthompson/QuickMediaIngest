#nullable enable
using System;
using QuickMediaIngest.Core.AppModel;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class IngestBenchImportProgressTests
{
    [Fact]
    public void Percent_ClampsAndScales()
    {
        Assert.Equal(0, IngestBenchImportProgress.Percent(0, 0));
        Assert.Equal(0, IngestBenchImportProgress.Percent(0, 10));
        Assert.Equal(50, IngestBenchImportProgress.Percent(5, 10));
        Assert.Equal(100, IngestBenchImportProgress.Percent(10, 10));
        Assert.Equal(100, IngestBenchImportProgress.Percent(12, 10));
    }

    [Fact]
    public void Clock_UsesMinutesUntilAnHour()
    {
        Assert.Equal("0:05", IngestBenchImportProgress.Clock(TimeSpan.FromSeconds(5)));
        Assert.Equal("1:01:01", IngestBenchImportProgress.Clock(new TimeSpan(1, 1, 1)));
    }

    [Fact]
    public void Eta_NeedsProgressAndElapsed()
    {
        Assert.Equal(string.Empty, IngestBenchImportProgress.Eta(0, 10, TimeSpan.FromSeconds(10)));
        Assert.Equal(string.Empty, IngestBenchImportProgress.Eta(5, 10, TimeSpan.FromMilliseconds(200)));
        Assert.Equal("0:10", IngestBenchImportProgress.Eta(5, 10, TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public void Line_ShowsOverallCountElapsedAndRemaining()
    {
        string early = IngestBenchImportProgress.Line("Importing", 0, 1500, TimeSpan.Zero);
        Assert.Contains("Importing 0/1500", early, StringComparison.Ordinal);
        Assert.Contains("elapsed", early, StringComparison.Ordinal);
        Assert.DoesNotContain("left", early, StringComparison.Ordinal);

        string later = IngestBenchImportProgress.Dual(412, 380, 1500, TimeSpan.FromSeconds(50));
        Assert.Contains("Copy 412/1500", later, StringComparison.Ordinal);
        Assert.Contains("Verify 380/1500", later, StringComparison.Ordinal);
        Assert.Contains("elapsed", later, StringComparison.Ordinal);
    }
}
