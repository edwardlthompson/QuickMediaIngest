#nullable enable
using System.IO;
using ImageMagick;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class IngestBenchThumbsTests
{
    [Fact]
    public void DecodeToCache_WritesJpeg_AndCapDeletesOldest()
    {
        var paths = new ThumbTempPaths();
        string src = Path.Combine(paths.AppDataRoot, "src.jpg");
        using (var image = new MagickImage(MagickColors.Red, 8, 8))
        {
            image.Write(src, MagickFormat.Jpeg);
        }

        string? cached = IngestBenchThumbs.DecodeToCache(paths, src, maxEdge: 32);
        Assert.False(string.IsNullOrWhiteSpace(cached));
        Assert.True(File.Exists(cached));

        string extra = Path.Combine(IngestBenchThumbs.CacheDir(paths), "old.bin");
        File.WriteAllBytes(extra, new byte[2000]);
        File.SetLastWriteTimeUtc(extra, System.DateTime.UtcNow.AddDays(-2));
        IngestBenchThumbs.EnforceCap(paths, capBytes: 500);
        Assert.False(File.Exists(extra));
    }

    [Fact]
    public void Purge_RemovesCacheFiles()
    {
        var paths = new ThumbTempPaths();
        string src = Path.Combine(paths.AppDataRoot, "src.jpg");
        using (var image = new MagickImage(MagickColors.Blue, 8, 8))
        {
            image.Write(src, MagickFormat.Jpeg);
        }

        using IngestBenchHost host = IngestBenchHost.Create(
            QuickMediaIngest.Core.Prompt.InlineUiDispatcher.Instance,
            paths,
            QuickMediaIngest.Core.Prompt.SilentUserPrompt.Instance);
        Assert.False(string.IsNullOrWhiteSpace(IngestBenchThumbs.DecodeToCache(paths, src, maxEdge: 16)));
        Assert.True(Directory.Exists(IngestBenchThumbs.CacheDir(paths)));
        IngestBenchThumbs.Purge(host);
        Assert.False(Directory.Exists(IngestBenchThumbs.CacheDir(paths)));
        Assert.False(host.Model.ShowCompare);
    }

    [Fact]
    public void HeifMissing_ReturnsNull()
    {
        Assert.Null(QuickMediaIngest.Core.Thumbnails.HeifPreviewDecoder.TryDecode("/no/such.heic"));
        _ = new QuickMediaIngest.Core.Services.ColorManagementProfileService().GetSystemDefaultIccProfilePath();
    }

    [Fact]
    public void DecodeItem_LocalJpeg_WritesPreviewCache()
    {
        var paths = new ThumbTempPaths();
        string src = Path.Combine(paths.AppDataRoot, "card.jpg");
        using (var image = new MagickImage(MagickColors.Green, 12, 12))
        {
            image.Write(src, MagickFormat.Jpeg);
        }

        using IngestBenchHost host = IngestBenchHost.Create(
            QuickMediaIngest.Core.Prompt.InlineUiDispatcher.Instance,
            paths,
            QuickMediaIngest.Core.Prompt.SilentUserPrompt.Instance);
        var item = new QuickMediaIngest.Core.Models.ImportItem { FileName = "card.jpg", SourcePath = src };
        host.Groups.Add(new QuickMediaIngest.Core.Models.ItemGroup { Title = "Local", Items = { item } });
        ThumbFillResult filled = IngestBenchThumbs.FillShootPreviews(host, perGroup: 4);
        Assert.True(File.Exists(item.PreviewCachePath));
        Assert.EndsWith(".jpg", item.PreviewCachePath);
        Assert.Equal(1, filled.Succeeded);
        Assert.Equal(0, filled.Failed);
    }

    [Fact]
    public void FillShootPreviews_LocalFiles_FillBeyondPerGroupCap()
    {
        var paths = new ThumbTempPaths();
        using IngestBenchHost host = IngestBenchHost.Create(
            QuickMediaIngest.Core.Prompt.InlineUiDispatcher.Instance,
            paths,
            QuickMediaIngest.Core.Prompt.SilentUserPrompt.Instance);
        var group = new QuickMediaIngest.Core.Models.ItemGroup { Title = "Card" };
        for (int i = 0; i < 6; i++)
        {
            string src = Path.Combine(paths.AppDataRoot, i + ".jpg");
            using (var image = new MagickImage(MagickColors.Green, 12, 12))
            {
                image.Write(src, MagickFormat.Jpeg);
            }

            group.Items.Add(new QuickMediaIngest.Core.Models.ImportItem
            {
                FileName = i + ".jpg",
                SourcePath = src,
            });
        }

        host.Groups.Add(group);
        ThumbFillResult filled = IngestBenchThumbs.FillShootPreviews(host, perGroup: 2);
        Assert.Equal(6, filled.Succeeded);
        Assert.Equal(0, filled.Failed);
        Assert.All(group.Items, item => Assert.True(File.Exists(item.PreviewCachePath)));
    }

    [Fact]
    public void FileIdentity_SurvivesRemountPrefixAndMtime()
    {
        Assert.Equal(
            IngestBenchThumbCache.VolumeRelative("/media/edward/CANON_DC/DCIM/236_0914/IMG_5321.CR2"),
            IngestBenchThumbCache.VolumeRelative("/run/media/edward/CANON_DC/DCIM/236_0914/IMG_5321.CR2"));

        var paths = new ThumbTempPaths();
        string src = Path.Combine(paths.AppDataRoot, "card.jpg");
        using (var image = new MagickImage(MagickColors.Green, 12, 12))
        {
            image.Write(src, MagickFormat.Jpeg);
        }

        string? first = IngestBenchThumbs.DecodeToCache(paths, src, maxEdge: 32);
        File.SetLastWriteTimeUtc(src, System.DateTime.UtcNow.AddDays(2));
        string? second = IngestBenchThumbs.DecodeToCache(paths, src, maxEdge: 32);
        Assert.Equal(first, second);
        Assert.Single(Directory.GetFiles(IngestBenchThumbs.CacheDir(paths), "*.jpg"));
    }

    [Fact]
    public void FillShootPreviews_LocalStillLoads_WhenPhoneGroupPresent()
    {
        var paths = new ThumbTempPaths();
        string src = Path.Combine(paths.AppDataRoot, "card.jpg");
        using (var image = new MagickImage(MagickColors.Green, 12, 12))
        {
            image.Write(src, MagickFormat.Jpeg);
        }

        using IngestBenchHost host = IngestBenchHost.Create(
            QuickMediaIngest.Core.Prompt.InlineUiDispatcher.Instance,
            paths,
            QuickMediaIngest.Core.Prompt.SilentUserPrompt.Instance);
        var phone = new QuickMediaIngest.Core.Models.ItemGroup { Title = "Phone" };
        phone.Items.Add(new QuickMediaIngest.Core.Models.ImportItem
        {
            FileName = "IMG_0001.JPG",
            SourcePath = "/sdcard/DCIM/IMG_0001.JPG",
            SourceId = "adb:deadbeef",
        });
        var card = new QuickMediaIngest.Core.Models.ItemGroup { Title = "Card" };
        var local = new QuickMediaIngest.Core.Models.ImportItem { FileName = "card.jpg", SourcePath = src };
        card.Items.Add(local);
        host.Groups.Add(phone);
        host.Groups.Add(card);
        ThumbFillResult filled = IngestBenchThumbs.FillShootPreviews(host, perGroup: 24);
        Assert.True(File.Exists(local.PreviewCachePath));
        Assert.Equal(1, filled.Succeeded);
        Assert.Equal(1, filled.Failed);
    }

    [Fact]
    public void FullJpeg_JpegSource_ReturnsOriginalNotGridCache()
    {
        var paths = new ThumbTempPaths();
        string src = Path.Combine(paths.AppDataRoot, "wide.jpg");
        using (var image = new MagickImage(MagickColors.Red, 2000, 1200))
        {
            image.Write(src, MagickFormat.Jpeg);
        }

        using IngestBenchHost host = IngestBenchHost.Create(
            QuickMediaIngest.Core.Prompt.InlineUiDispatcher.Instance,
            paths,
            QuickMediaIngest.Core.Prompt.SilentUserPrompt.Instance);
        var item = new QuickMediaIngest.Core.Models.ImportItem { FileName = "wide.jpg", SourcePath = src };
        host.Groups.Add(new QuickMediaIngest.Core.Models.ItemGroup { Title = "Local", Items = { item } });
        _ = IngestBenchThumbs.FillShootPreviews(host, perGroup: 4);
        Assert.True(File.Exists(item.PreviewCachePath));
        string? full = IngestBenchThumbs.FullJpeg(host, item);
        Assert.Equal(src, full);
        using var original = new MagickImage(full!);
        using var grid = new MagickImage(item.PreviewCachePath);
        Assert.True(original.Width > grid.Width);
        Assert.Equal(2000u, original.Width);
    }

    [Fact]
    public void FullJpeg_CanonCr2_KeepsEmbeddedJpegLargerThanGrid()
    {
        string cr2 = "/media/edward/CANON_DC/DCIM/236_0914/IMG_5321.CR2";
        if (!File.Exists(cr2))
        {
            return;
        }

        var paths = new ThumbTempPaths();
        using IngestBenchHost host = IngestBenchHost.Create(
            QuickMediaIngest.Core.Prompt.InlineUiDispatcher.Instance,
            paths,
            QuickMediaIngest.Core.Prompt.SilentUserPrompt.Instance);
        var item = new QuickMediaIngest.Core.Models.ImportItem { FileName = "IMG_5321.CR2", SourcePath = cr2 };
        string? grid = IngestBenchThumbs.DecodeToCache(paths, cr2, maxEdge: 256);
        string? full = IngestBenchThumbs.FullJpeg(host, item);
        Assert.False(string.IsNullOrWhiteSpace(grid));
        Assert.False(string.IsNullOrWhiteSpace(full));
        Assert.NotEqual(grid, full);
        using var preview = new MagickImage(full!);
        Assert.True(preview.Width >= 1600);
        Assert.True(new FileInfo(full!).Length > new FileInfo(grid!).Length);
    }

    [Fact]
    public void DecodeToCache_CanonCr2_WritesSmallTileNotFullPreview()
    {
        string cr2 = "/media/edward/CANON_DC/DCIM/236_0914/IMG_5321.CR2";
        if (!File.Exists(cr2))
        {
            return;
        }

        var paths = new ThumbTempPaths();
        string? cached = IngestBenchThumbs.DecodeToCache(paths, cr2, maxEdge: 256);
        Assert.False(string.IsNullOrWhiteSpace(cached));
        Assert.True(File.Exists(cached));
        Assert.InRange(new FileInfo(cached!).Length, 64, 200_000);
        using var image = new MagickImage(cached);
        Assert.True(image.Width <= 256);
        Assert.True(image.Height <= 256);
    }

    [Fact]
    public void TryExisting_RejectsOversizedCacheJpeg()
    {
        var paths = new ThumbTempPaths();
        string identity = "file|/tmp/fake.cr2";
        Directory.CreateDirectory(IngestBenchThumbs.CacheDir(paths));
        string dest = IngestBenchThumbCache.JpegPath(paths, identity, 256);
        File.WriteAllBytes(dest, new byte[400_000]);
        Assert.Null(IngestBenchThumbCache.TryExisting(paths, identity, 256));
    }

    [Fact]
    public void RawEmbeddedPreviewReader_CanonCr2_ExtractsJpegWhenCardMounted()
    {
        string cr2 = "/media/edward/CANON_DC/DCIM/236_0914/IMG_5321.CR2";
        if (!File.Exists(cr2))
        {
            return;
        }

        DecodedThumbnail? thumb = RawEmbeddedPreviewReader.TryExtract(cr2);
        Assert.NotNull(thumb);
        Assert.True(thumb!.JpegBytes.Length > 2048);
        Assert.True(thumb.Width >= 32);
        Assert.True(thumb.Height >= 32);
    }

    [Fact]
    public void FillShootPreviews_CountsSucceededAndFailed()
    {
        var paths = new ThumbTempPaths();
        string src = Path.Combine(paths.AppDataRoot, "ok.jpg");
        using (var image = new MagickImage(MagickColors.Green, 12, 12))
        {
            image.Write(src, MagickFormat.Jpeg);
        }

        using IngestBenchHost host = IngestBenchHost.Create(
            QuickMediaIngest.Core.Prompt.InlineUiDispatcher.Instance,
            paths,
            QuickMediaIngest.Core.Prompt.SilentUserPrompt.Instance);
        var ok = new QuickMediaIngest.Core.Models.ImportItem { FileName = "ok.jpg", SourcePath = src };
        var miss = new QuickMediaIngest.Core.Models.ImportItem { FileName = "miss.jpg", SourcePath = "/no/file" };
        host.Groups.Add(new QuickMediaIngest.Core.Models.ItemGroup { Title = "Mix", Items = { ok, miss } });
        ThumbFillResult filled = IngestBenchThumbs.FillShootPreviews(host, perGroup: 4);
        Assert.Equal(1, filled.Succeeded);
        Assert.Equal(1, filled.Failed);
        Assert.True(File.Exists(ok.PreviewCachePath));
        Assert.True(string.IsNullOrEmpty(miss.PreviewCachePath));
    }

    [Fact]
    public void DecodeToCache_ReusesStablePath()
    {
        var paths = new ThumbTempPaths();
        string src = Path.Combine(paths.AppDataRoot, "src.jpg");
        using (var image = new MagickImage(MagickColors.Red, 8, 8))
        {
            image.Write(src, MagickFormat.Jpeg);
        }

        string? first = IngestBenchThumbs.DecodeToCache(paths, src, maxEdge: 32);
        string? second = IngestBenchThumbs.DecodeToCache(paths, src, maxEdge: 32);
        Assert.Equal(first, second);
        Assert.Single(Directory.GetFiles(IngestBenchThumbs.CacheDir(paths), "*.jpg"));
    }

    [Theory]
    [InlineData("wide.avif", MagickFormat.Avif)]
    [InlineData("wide.jxl", MagickFormat.Jxl)]
    public void DecodeToCache_AvifAndJxl_WritesJpegWhenMagickWrites(string name, MagickFormat format)
    {
        var paths = new ThumbTempPaths();
        string src = Path.Combine(paths.AppDataRoot, name);
        try
        {
            using (var image = new MagickImage(MagickColors.Blue, 10, 10))
            {
                image.Write(src, format);
            }
        }
        catch (MagickException)
        {
            return;
        }

        if (!File.Exists(src) || new FileInfo(src).Length < 64)
        {
            return;
        }

        string? cached = IngestBenchThumbs.DecodeToCache(paths, src, maxEdge: 32);
        Assert.False(string.IsNullOrWhiteSpace(cached));
        Assert.True(File.Exists(cached));
        Assert.EndsWith(".jpg", cached);
    }

    [Fact]
    public void ForgetImported_DeletesCachedJpeg()
    {
        var paths = new ThumbTempPaths();
        string src = Path.Combine(paths.AppDataRoot, "card.jpg");
        using (var image = new MagickImage(MagickColors.Green, 12, 12))
        {
            image.Write(src, MagickFormat.Jpeg);
        }

        using IngestBenchHost host = IngestBenchHost.Create(
            QuickMediaIngest.Core.Prompt.InlineUiDispatcher.Instance,
            paths,
            QuickMediaIngest.Core.Prompt.SilentUserPrompt.Instance);
        var item = new QuickMediaIngest.Core.Models.ImportItem { FileName = "card.jpg", SourcePath = src };
        host.Groups.Add(new QuickMediaIngest.Core.Models.ItemGroup { Title = "Local", Items = { item } });
        IngestBenchThumbs.FillShootPreviews(host, perGroup: 4);
        string cached = item.PreviewCachePath;
        Assert.True(File.Exists(cached));
        IngestBenchThumbs.ForgetImported(host, new[] { item });
        Assert.False(File.Exists(cached));
        Assert.True(string.IsNullOrEmpty(item.PreviewCachePath));
    }

    [Fact]
    public async System.Threading.Tasks.Task FillShootPreviews_DoesNotThrow_WhenGroupsMutated()
    {
        var paths = new ThumbTempPaths();
        using IngestBenchHost host = IngestBenchHost.Create(
            QuickMediaIngest.Core.Prompt.InlineUiDispatcher.Instance,
            paths,
            QuickMediaIngest.Core.Prompt.SilentUserPrompt.Instance);
        var group = new QuickMediaIngest.Core.Models.ItemGroup { Title = "Live" };
        for (int i = 0; i < 32; i++)
        {
            group.Items.Add(new QuickMediaIngest.Core.Models.ImportItem { FileName = i + ".jpg", SourcePath = "/no/file" });
        }

        host.Groups.Add(group);
        Exception? error = null;
        var fill = System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                IngestBenchThumbs.FillShootPreviews(host, perGroup: 4);
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        host.Groups.Clear();
        group.Items.Clear();
        await fill;
        Assert.Null(error);
    }

    [Fact]
    public void AdbPreviewPull_MissingSerial_ReturnsFalse()
    {
        string dest = Path.Combine(Path.GetTempPath(), "qmi-adb-miss-" + Path.GetRandomFileName() + ".jpg");
        Assert.False(AdbPreviewPull.TryPull("no-such-serial", "/sdcard/DCIM/none.jpg", dest));
        Assert.False(File.Exists(dest));
    }
}

file sealed class ThumbTempPaths : IAppPaths
{
    public ThumbTempPaths()
    {
        AppDataRoot = Path.Combine(Path.GetTempPath(), "qmi-thumbs-" + Path.GetRandomFileName());
        Directory.CreateDirectory(AppDataRoot);
    }

    public string AppDataRoot { get; }
}
