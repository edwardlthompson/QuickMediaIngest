#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Prompt;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class IngestBenchPostTests
{
    [Fact]
    public async Task Import_WritesChecksumManifestAndSecondaryAndXmp()
    {
        string src = Path.Combine(Path.GetTempPath(), "qmi-post-src-" + Path.GetRandomFileName());
        string dest = Path.Combine(Path.GetTempPath(), "qmi-post-dst-" + Path.GetRandomFileName());
        string sec = Path.Combine(Path.GetTempPath(), "qmi-post-sec-" + Path.GetRandomFileName());
        Directory.CreateDirectory(src);
        Directory.CreateDirectory(dest);
        Directory.CreateDirectory(sec);
        File.WriteAllBytes(Path.Combine(src, "shot.jpg"), new byte[] { 9, 8, 7, 6 });
        var paths = new PostPaths();
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        try
        {
            SelectOnly(host, src);
            host.SetDestination(dest);
            host.Model.Prefs.SecondaryDestination = sec;
            host.Model.Prefs.CopyrightStamp = "Copyright (c) 2026 Test";
            host.Model.Prefs.CreatorStamp = "QMI";
            host.Model.Prefs.WriteChecksumManifest = true;
            host.Model.Prefs.WriteXmpSidecar = true;
            Assert.True(await host.RunImportAsync(dryRun: false, IngestBenchCopy.Neutral));
            Assert.NotEmpty(Directory.EnumerateFiles(dest, "checksums.sha256", SearchOption.AllDirectories));
            Assert.NotEmpty(Directory.EnumerateFiles(sec, "*.jpg", SearchOption.AllDirectories));
            Assert.NotEmpty(Directory.EnumerateFiles(dest, "*.xmp", SearchOption.AllDirectories));
            Assert.True(File.Exists(IngestBenchPost.CatalogFile(paths)));
        }
        finally
        {
            TryDelete(src);
            TryDelete(dest);
            TryDelete(sec);
        }
    }

    [Fact]
    public async Task SkipAlreadyImported_LeavesMissingSourceSelected()
    {
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, new PostPaths(), SilentUserPrompt.Instance);
        var group = new QuickMediaIngest.Core.Models.ItemGroup { Title = "Adb" };
        var item = new QuickMediaIngest.Core.Models.ImportItem
        {
            SourcePath = Path.Combine(Path.GetTempPath(), "qmi-missing-" + Path.GetRandomFileName() + ".jpg"),
            FileName = "remote.jpg",
            IsSelected = true,
        };
        group.Items.Add(item);
        await IngestBenchPost.SkipAlreadyImportedAsync(host, new[] { group });
        Assert.True(item.IsSelected);
    }

    [Fact]
    public async Task SecondImport_SkipsAlreadyHashedSource()
    {
        string src = Path.Combine(Path.GetTempPath(), "qmi-hash-src-" + Path.GetRandomFileName());
        string dest = Path.Combine(Path.GetTempPath(), "qmi-hash-dst-" + Path.GetRandomFileName());
        Directory.CreateDirectory(src);
        Directory.CreateDirectory(dest);
        File.WriteAllBytes(Path.Combine(src, "shot.jpg"), new byte[] { 1, 1, 1, 1 });
        var paths = new PostPaths();
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        try
        {
            SelectOnly(host, src);
            host.SetDestination(dest);
            Assert.True(await host.RunImportAsync(dryRun: false, IngestBenchCopy.Neutral));
            int first = Directory.EnumerateFiles(dest, "*.jpg", SearchOption.AllDirectories).Count();
            Assert.True(await host.RunImportAsync(dryRun: false, IngestBenchCopy.Neutral));
            int second = Directory.EnumerateFiles(dest, "*.jpg", SearchOption.AllDirectories).Count();
            Assert.Equal(first, second);
        }
        finally
        {
            TryDelete(src);
            TryDelete(dest);
        }
    }

    private static void SelectOnly(IngestBenchHost host, string root)
    {
        host.BeginDrivePick(new[] { root });
        string full = Path.GetFullPath(root);
        foreach (VolumeChoice volume in host.Model.Volumes)
        {
            volume.IsSelected = string.Equals(volume.Path, full, StringComparison.Ordinal);
        }

        host.ConfirmDrivePick();
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

file sealed class PostPaths : IAppPaths
{
    public PostPaths()
    {
        AppDataRoot = Path.Combine(Path.GetTempPath(), "qmi-post-app-" + Path.GetRandomFileName());
        Directory.CreateDirectory(AppDataRoot);
    }

    public string AppDataRoot { get; }
}
