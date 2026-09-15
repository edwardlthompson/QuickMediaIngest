#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Prompt;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class IngestBenchShootsTests
{
    [Fact]
    public async Task SkipSelected_PreventsImportCopy()
    {
        string src = Path.Combine(Path.GetTempPath(), "qmi-sh-src-" + Path.GetRandomFileName());
        string dest = Path.Combine(Path.GetTempPath(), "qmi-sh-dst-" + Path.GetRandomFileName());
        Directory.CreateDirectory(src);
        Directory.CreateDirectory(dest);
        File.WriteAllBytes(Path.Combine(src, "shot.jpg"), new byte[] { 1, 2, 3 });
        var paths = new ShootTempPaths();
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
            Assert.NotEmpty(host.Model.Shoots);
            Assert.Equal("Local", host.Model.Shoots[0].TransportDisplay);
            IngestBenchShoots.SkipVisibleSelected(host);
            host.SetDestination(dest);
            Assert.False(await host.RunImportAsync(dryRun: false, IngestBenchCopy.Neutral));
            Assert.Empty(Directory.EnumerateFiles(dest, "*.jpg", SearchOption.AllDirectories));
        }
        finally
        {
            TryDelete(src);
            TryDelete(dest);
        }
    }

    [Fact]
    public void KeywordFilter_HidesNonMatchingShoot()
    {
        string src = Path.Combine(Path.GetTempPath(), "qmi-filt-" + Path.GetRandomFileName());
        Directory.CreateDirectory(src);
        File.WriteAllBytes(Path.Combine(src, "shot.jpg"), new byte[] { 1 });
        var paths = new ShootTempPaths();
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        try
        {
            host.BeginDrivePick(new[] { src });
            foreach (VolumeChoice volume in host.Model.Volumes)
            {
                volume.IsSelected = string.Equals(volume.Path, Path.GetFullPath(src), StringComparison.Ordinal);
            }

            host.ConfirmDrivePick();
            Assert.Single(host.Model.Shoots);
            host.Model.ShootFilter = "no-such-keyword";
            IngestBenchShoots.ApplyFilter(host);
            Assert.Empty(host.Model.Shoots);
            IngestBenchShoots.SelectAll(host, true);
            Assert.True(host.Groups.All(g => g.IsSelected));
            IngestBenchShoots.ToggleSelectAll(host);
            Assert.True(host.Groups.All(g => !g.IsSelected));
            IngestBenchShoots.ToggleSelectAll(host);
            Assert.True(host.Groups.All(g => g.IsSelected));
            ItemGroup shoot = host.Groups[0];
            Assert.True(shoot.IsExpanded);
            IngestBenchShoots.ToggleExpanded(shoot);
            Assert.False(shoot.IsExpanded);
            IngestBenchShoots.SetAllExpanded(host, true);
            Assert.True(shoot.IsExpanded);
            Assert.True(shoot.FileCount >= 1);
            shoot.Title = "Studio";
            shoot.KeywordsText = "red";
            Assert.Equal("Studio", shoot.Title);
        }
        finally
        {
            TryDelete(src);
        }
    }

    [Fact]
    public void IgnoreFolder_AddsExclusionAndDeselects()
    {
        string src = Path.Combine(Path.GetTempPath(), "qmi-ign-" + Path.GetRandomFileName());
        Directory.CreateDirectory(src);
        File.WriteAllBytes(Path.Combine(src, "shot.jpg"), new byte[] { 1 });
        var paths = new ShootTempPaths();
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        try
        {
            host.BeginDrivePick(new[] { src });
            foreach (VolumeChoice volume in host.Model.Volumes)
            {
                volume.IsSelected = string.Equals(volume.Path, Path.GetFullPath(src), StringComparison.Ordinal);
            }

            host.ConfirmDrivePick();
            ItemGroup shoot = Assert.Single(host.Model.Shoots);
            IngestBenchShoots.IgnoreFolder(host, shoot);
            Assert.NotEmpty(host.Model.ExcludedFolders);
            Assert.Empty(host.Model.Shoots);
        }
        finally
        {
            TryDelete(src);
        }
    }

    [Fact]
    public void DropImported_RemovesSucceeded_KeepsFailed()
    {
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, new ShootTempPaths(), SilentUserPrompt.Instance);
        var ok = new ImportItem { FileName = "ok.jpg", SourcePath = "/tmp/ok.jpg", IsSelected = true };
        var fail = new ImportItem { FileName = "fail.jpg", SourcePath = "/tmp/fail.jpg", IsSelected = true };
        var skipped = new ImportItem { FileName = "skip.jpg", SourcePath = "/tmp/skip.jpg", IsSelected = false };
        var group = new ItemGroup { Title = "Shoot" };
        group.Items.Add(ok);
        group.Items.Add(fail);
        group.Items.Add(skipped);
        host.Groups.Add(group);
        host.Model.Shoots.Add(group);
        host.Model.NoteSources(3);
        host.Model.SelectedShoot = group;
        IngestBenchShoots.DropImported(host, new[] { group }, new[] { "/tmp/fail.jpg" });
        Assert.Single(host.Groups);
        Assert.Equal(2, host.Groups[0].Items.Count);
        Assert.DoesNotContain(host.Groups[0].Items, i => i.FileName == "ok.jpg");
        Assert.Contains(host.Groups[0].Items, i => i.FileName == "fail.jpg");
        Assert.Contains(host.Groups[0].Items, i => i.FileName == "skip.jpg");
        Assert.Equal(2, host.Model.SourceCount);
    }

    [Fact]
    public void DropOne_RemovesFile_RetargetsSelectedCull()
    {
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, new ShootTempPaths(), SilentUserPrompt.Instance);
        var first = new ImportItem { FileName = "a.jpg", SourcePath = "/tmp/a.jpg", IsSelected = true };
        var second = new ImportItem { FileName = "b.jpg", SourcePath = "/tmp/b.jpg", IsSelected = true };
        var group = new ItemGroup { Title = "Shoot" };
        group.Items.Add(first);
        group.Items.Add(second);
        host.Groups.Add(group);
        host.Model.Shoots.Add(group);
        host.Model.SelectedShoot = group;
        host.Model.SelectedCullItem = first;
        host.Model.NoteSources(2);
        Assert.False(IngestBenchShootsDrop.DropOne(host, "/tmp/a.jpg", refresh: true));
        Assert.Single(group.Items);
        Assert.Equal(second, host.Model.SelectedCullItem);
        Assert.Equal(1, host.Model.SourceCount);
        Assert.False(IngestBenchShootsDrop.DropOne(host, "/tmp/missing.jpg", refresh: true));
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

file sealed class ShootTempPaths : IAppPaths
{
    public ShootTempPaths()
    {
        AppDataRoot = Path.Combine(Path.GetTempPath(), "qmi-sh-app-" + Path.GetRandomFileName());
        Directory.CreateDirectory(AppDataRoot);
    }

    public string AppDataRoot { get; }
}
