#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageMagick;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Prompt;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class IngestBenchScanTests
{
    [Fact]
    public void ApplyMerged_IncludesLocalAndAdb_AndThumbsLocalJpeg()
    {
        var paths = new ScanTempPaths();
        string root = Path.Combine(paths.AppDataRoot, "card");
        Directory.CreateDirectory(root);
        string src = Path.Combine(root, "studio.jpg");
        using (var image = new MagickImage(MagickColors.Orange, 16, 16))
        {
            image.Write(src, MagickFormat.Jpeg);
        }

        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        List<ImportItem> local = IngestBenchImport.CollectItems(new[] { root });
        Assert.Single(local);
        var adbItem = new ImportItem
        {
            FileName = "phone.jpg",
            SourcePath = "/sdcard/DCIM/phone.jpg",
            SourceId = "adb:no-such-serial",
        };
        var adb = new IngestBenchAdb.AdbScanResult(
            1,
            new List<ImportItem> { adbItem },
            new List<VolumeChoice> { new("adb:no-such-serial", "no-such-serial", "ADB") },
            "adb");
        IngestBenchScan.ApplyMerged(
            host,
            new List<VolumeChoice> { new(root, "card", "Card") },
            local,
            adb);
        Assert.Equal(2, host.Model.SourceCount);
        Assert.Contains(host.Model.Volumes, v => v.Kind == "ADB");
        Assert.Contains("local", host.Model.ImportStatus);
        IngestBenchThumbs.FillShootPreviews(host, perGroup: 4);
        ImportItem? localItem = host.Groups.SelectMany(g => g.Items).First(i => i.FileName == "studio.jpg");
        Assert.True(File.Exists(localItem.PreviewCachePath));
    }
}

file sealed class ScanTempPaths : IAppPaths
{
    public ScanTempPaths()
    {
        AppDataRoot = Path.Combine(Path.GetTempPath(), "qmi-scan-" + Path.GetRandomFileName());
        Directory.CreateDirectory(AppDataRoot);
    }

    public string AppDataRoot { get; }
}
