#nullable enable
using System;
using System.IO;
using System.Linq;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Prompt;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class IngestBenchWatchTests
{
    [Fact]
    public void SplitMergeRename_UpdatesShoots()
    {
        string src = Path.Combine(Path.GetTempPath(), "qmi-w-src-" + Path.GetRandomFileName());
        Directory.CreateDirectory(src);
        File.WriteAllBytes(Path.Combine(src, "a.jpg"), new byte[] { 1 });
        File.WriteAllBytes(Path.Combine(src, "b.jpg"), new byte[] { 2 });
        var paths = new WatchPaths();
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        try
        {
            host.BeginDrivePick(new[] { src });
            string full = Path.GetFullPath(src);
            foreach (VolumeChoice volume in host.Model.Volumes)
            {
                volume.IsSelected = string.Equals(volume.Path, full, StringComparison.Ordinal);
            }

            host.ConfirmDrivePick();
            Assert.Single(host.Groups);
            host.Model.SelectedShoot = host.Groups[0];
            Assert.True(IngestBenchWatch.SplitSelected(host));
            Assert.Equal(2, host.Groups.Count);
            host.Model.SelectedShoot = host.Groups[0];
            Assert.True(IngestBenchWatch.MergeSelected(host));
            Assert.Single(host.Groups);
            host.Model.Prefs.BatchRenameBase = "Wedding";
            IngestBenchWatch.RenameVisible(host);
            Assert.Equal("Wedding", host.Model.Shoots[0].Title);
        }
        finally
        {
            TryDelete(src);
        }
    }

    [Fact]
    public void DestFolderTemplate_ResolvesTokens()
    {
        var group = new ItemGroup
        {
            Title = "Wedding Reception",
            StartDate = new DateTime(2026, 8, 30, 14, 0, 0),
        };
        string folder = GroupFolderNaming.GetTargetFolderName(
            group,
            template: "[Date]_[Client]_[Job]_[Camera]_[ShootTitle]",
            job: "Event402",
            client: "Smith",
            camera: "A7IV");
        Assert.Equal("2026-08-30_Smith_Event402_A7IV_Wedding Reception", folder);
        var options = IngestBenchPost.CreateOptions(new IngestBenchAppModel { DestFolderTemplate = "[ShootTitle]" }, dryRun: false);
        Assert.Equal("[ShootTitle]", options.DestinationFolderTemplate);
    }

    [Fact]
    public void TimeZone_UtcMarksKind()
    {
        var item = new ImportItem { DateTaken = new DateTime(2026, 8, 30, 10, 0, 0) };
        DateTimeZoneAdjuster.ApplyTimeZoneOverride(new[] { item }, TimeZoneOverrideMode.Utc);
        Assert.Equal(DateTimeKind.Utc, item.DateTaken.Kind);
    }

    [Fact]
    public void WatchFolderService_StartStop()
    {
        string dir = Path.Combine(Path.GetTempPath(), "qmi-watch-" + Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            using var service = new WatchFolderService(Microsoft.Extensions.Logging.Abstractions.NullLogger<WatchFolderService>.Instance);
            service.StartWatching(dir);
            Assert.True(service.IsWatching);
            service.StopWatching();
            Assert.False(service.IsWatching);
        }
        finally
        {
            TryDelete(dir);
        }
    }

    private static void TryDelete(string dir)
    {
        try
        {
            Directory.Delete(dir, recursive: true);
        }
        catch
        {
            // temp
        }
    }
}

file sealed class WatchPaths : IAppPaths
{
    public WatchPaths()
    {
        AppDataRoot = Path.Combine(Path.GetTempPath(), "qmi-w-app-" + Path.GetRandomFileName());
        Directory.CreateDirectory(AppDataRoot);
    }

    public string AppDataRoot { get; }
}
