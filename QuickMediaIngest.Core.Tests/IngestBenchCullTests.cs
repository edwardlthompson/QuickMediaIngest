#nullable enable
using System;
using System.IO;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Prompt;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class IngestBenchCullTests
{
    [Fact]
    public void PickAndReject_UpdatesSelection()
    {
        var item = new ImportItem { FileName = "IMG_001.JPG", IsSelected = true };
        item.Reject();
        Assert.True(item.IsRejected);
        Assert.False(item.IsSelected);
        item.Pick();
        Assert.False(item.IsRejected);
        Assert.True(item.IsSelected);
    }

    [Fact]
    public void Rating_ClampsAndColorPersists()
    {
        var item = new ImportItem { FileName = "IMG_002.JPG", Rating = 5, ColorLabel = "Green" };
        Assert.Equal(5, item.Rating);
        Assert.Equal("Green", item.ColorLabel);
        item.Rating = 10;
        Assert.Equal(5, item.Rating);
        item.Rating = -1;
        Assert.Equal(0, item.Rating);
    }

    [Fact]
    public void SnapshotAndRestore_PreservesCullAcrossRescan()
    {
        CullSelectionPersistence.Clear();
        var original = new ImportItem
        {
            FileName = "DSC-uniq.JPG",
            FileSize = 5000,
            DateTaken = new DateTime(2026, 8, 30, 9, 0, 0),
            IsSelected = false,
            IsRejected = true,
            Rating = 4,
            ColorLabel = "Red",
        };
        CullSelectionPersistence.Snapshot(new[] { original });
        var rescanned = new ImportItem
        {
            FileName = "DSC-uniq.JPG",
            FileSize = 5000,
            DateTaken = new DateTime(2026, 8, 30, 9, 0, 0),
            IsSelected = true,
            IsRejected = false,
            Rating = 0,
            ColorLabel = "",
        };
        CullSelectionPersistence.Restore(new[] { rescanned });
        Assert.False(rescanned.IsSelected);
        Assert.True(rescanned.IsRejected);
        Assert.Equal(4, rescanned.Rating);
        Assert.Equal("Red", rescanned.ColorLabel);
        CullSelectionPersistence.Clear();
    }

    [Fact]
    public void RefreshScan_RestoresRejectedItem()
    {
        CullSelectionPersistence.Clear();
        string src = Path.Combine(Path.GetTempPath(), "qmi-cull-src-" + Path.GetRandomFileName());
        Directory.CreateDirectory(src);
        string name = "cull-" + Path.GetRandomFileName() + ".jpg";
        File.WriteAllBytes(Path.Combine(src, name), new byte[] { 7, 7, 7 });
        var paths = new CullPaths();
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
            Assert.NotEmpty(host.Groups);
            ImportItem item = host.Groups[0].Items[0];
            host.Model.SelectedCullItem = item;
            IngestBenchCull.RejectSelected(host);
            Assert.True(item.IsRejected);
            host.RefreshScan();
            Assert.True(host.Groups[0].Items[0].IsRejected);
            Assert.False(host.Groups[0].Items[0].IsSelected);
            host.Model.SelectedCullItem = host.Groups[0].Items[0];
            IngestBenchCull.SetRating(host, 3);
            IngestBenchCull.SetColor(host, "Blue");
            host.RefreshScan();
            Assert.Equal(3, host.Groups[0].Items[0].Rating);
            Assert.Equal("Blue", host.Groups[0].Items[0].ColorLabel);
        }
        finally
        {
            CullSelectionPersistence.Clear();
            try
            {
                Directory.Delete(src, recursive: true);
            }
            catch
            {
                // temp
            }
        }
    }
}

file sealed class CullPaths : IAppPaths
{
    public CullPaths()
    {
        AppDataRoot = Path.Combine(Path.GetTempPath(), "qmi-cull-app-" + Path.GetRandomFileName());
        Directory.CreateDirectory(AppDataRoot);
    }

    public string AppDataRoot { get; }
}
