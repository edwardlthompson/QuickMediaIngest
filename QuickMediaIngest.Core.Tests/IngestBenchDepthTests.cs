#nullable enable
using System.Collections.Generic;
using System.IO;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Prompt;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class IngestBenchDepthTests
{
    [Fact]
    public void Filters_FileTypeAndChips()
    {
        var jpg = new ImportItem { FileName = "a.jpg" };
        var mov = new ImportItem { FileName = "b.mp4", IsVideo = true };
        Assert.True(IngestBenchFilters.PassesFileType(jpg, "Images"));
        Assert.False(IngestBenchFilters.PassesFileType(mov, "Images"));
        Assert.True(IngestBenchFilters.PassesFileType(mov, "Videos"));
        var model = new IngestBenchAppModel { ShootFilter = "red", FilterFileType = "RAW" };
        IngestBenchFilters.RefreshChips(model);
        Assert.Equal(2, model.FilterChips.Count);
    }

    [Fact]
    public void PreviewStack_HidesRawWhenGrouped()
    {
        var items = new List<ImportItem>
        {
            new() { FileName = "img.CR3", SourcePath = "/tmp/img.CR3" },
            new() { FileName = "img.jpg", SourcePath = "/tmp/img.jpg" },
        };
        PreviewStackApplier.Apply(items, groupPairs: true, expandStacks: false);
        Assert.True(items[1].IsPreviewVisible);
        Assert.False(items[0].IsPreviewVisible);
    }

    [Fact]
    public void DuplicatePolicy_MapsToEnum()
    {
        Assert.Equal(DuplicateHandlingMode.Skip, IngestBenchPost.Duplicates("Skip"));
        Assert.Equal(DuplicateHandlingMode.OverwriteIfNewer, IngestBenchPost.Duplicates("OverwriteIfNewer"));
        Assert.Equal(DuplicateHandlingMode.Suffix, IngestBenchPost.Duplicates("Suffix"));
        IngestOptions options = IngestBenchPost.CreateOptions(
            new IngestBenchAppModel { Prefs = { DuplicatePolicy = "Skip", VerificationMode = "Strict" } },
            dryRun: true);
        Assert.Equal(DuplicateHandlingMode.Skip, options.DuplicateHandling);
        Assert.Equal(ImportVerificationMode.Strict, options.VerificationMode);
    }

    [Fact]
    public void Prefs_RoundTripImportSettings()
    {
        string root = Path.Combine(Path.GetTempPath(), "qmi-imp-" + Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        var paths = new DepthPaths(root);
        IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        host.Model.Prefs.ConfirmBeforeImport = true;
        host.Model.Prefs.GroupRawJpeg = true;
        host.Model.Prefs.DuplicatePolicy = "Skip";
        IngestBenchPrefs.Save(host);
        IngestBenchHost again = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        Assert.True(again.Model.Prefs.ConfirmBeforeImport);
        Assert.True(again.Model.Prefs.GroupRawJpeg);
        Assert.Equal("Skip", again.Model.Prefs.DuplicatePolicy);
    }

    [Fact]
    public void FillShootPreviews_WritesCachePath()
    {
        var paths = new DepthPaths(Path.Combine(Path.GetTempPath(), "qmi-th2-" + Path.GetRandomFileName()));
        Directory.CreateDirectory(paths.AppDataRoot);
        string src = Path.Combine(paths.AppDataRoot, "s.jpg");
        using (var image = new ImageMagick.MagickImage(ImageMagick.MagickColors.Red, 8, 8))
        {
            image.Write(src, ImageMagick.MagickFormat.Jpeg);
        }

        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        host.Groups.Add(new ItemGroup { Items = { new ImportItem { FileName = "s.jpg", SourcePath = src } } });
        IngestBenchThumbs.FillShootPreviews(host, perGroup: 4);
        Assert.False(string.IsNullOrWhiteSpace(host.Groups[0].Items[0].PreviewCachePath));
        Assert.True(File.Exists(host.Groups[0].Items[0].PreviewCachePath));
    }

    private sealed class DepthPaths : IAppPaths
    {
        public DepthPaths(string root) => AppDataRoot = root;

        public string AppDataRoot { get; }
    }
}
