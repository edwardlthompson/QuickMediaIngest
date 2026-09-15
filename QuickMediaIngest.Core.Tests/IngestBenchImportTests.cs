#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Prompt;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class IngestBenchImportTests
{
    [Fact]
    public async Task DryRun_LeavesSource_ImportCopiesJpeg()
    {
        string src = Path.Combine(Path.GetTempPath(), "qmi-imp-src-" + Path.GetRandomFileName());
        string dest = Path.Combine(Path.GetTempPath(), "qmi-imp-dst-" + Path.GetRandomFileName());
        Directory.CreateDirectory(src);
        Directory.CreateDirectory(dest);
        string photo = Path.Combine(src, "shot.jpg");
        File.WriteAllBytes(photo, new byte[] { 1, 2, 3, 4 });
        var paths = new ImportTempPaths();
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        try
        {
            SelectOnly(host, src);
            host.SetDestination(dest);
            var statuses = new List<string>();
            host.Model.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(host.Model.ImportStatus) && !string.IsNullOrWhiteSpace(host.Model.ImportStatus))
                {
                    statuses.Add(host.Model.ImportStatus);
                }
            };
            Assert.True(await host.RunImportAsync(dryRun: true, IngestBenchCopy.Neutral));
            Assert.True(File.Exists(photo));
            Assert.Empty(Directory.EnumerateFiles(dest, "*.jpg", SearchOption.AllDirectories));

            Assert.True(await host.RunImportAsync(dryRun: false, IngestBenchCopy.Neutral));
            Assert.True(File.Exists(photo));
            Assert.NotEmpty(Directory.EnumerateFiles(dest, "*.jpg", SearchOption.AllDirectories));
            Assert.Equal("Import 1 succeeded, 0 failed.", host.Model.ImportStatus);
            Assert.Contains(statuses, s => s.Contains("Copy ", System.StringComparison.Ordinal) && s.Contains("Verify ", System.StringComparison.Ordinal));
            Assert.Contains(statuses, s => s.Contains("elapsed", StringComparison.Ordinal));
            Assert.Empty(host.Groups);
            Assert.Equal(0, host.Model.SourceCount);
            Assert.False(host.Model.CanImport);
        }
        finally
        {
            TryDelete(src);
            TryDelete(dest);
        }
    }

    [Fact]
    public async Task DeleteAfter_TrashesSource()
    {
        string src = Path.Combine(Path.GetTempPath(), "qmi-del-src-" + Path.GetRandomFileName());
        string dest = Path.Combine(Path.GetTempPath(), "qmi-del-dst-" + Path.GetRandomFileName());
        Directory.CreateDirectory(src);
        Directory.CreateDirectory(dest);
        string photo = Path.Combine(src, "shot.jpg");
        File.WriteAllBytes(photo, new byte[] { 9, 9, 9 });
        var paths = new ImportTempPaths();
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        try
        {
            SelectOnly(host, src);
            host.SetDestination(dest);
            host.Model.DeleteAfterImport = true;
            Assert.True(await host.RunImportAsync(dryRun: false, IngestBenchCopy.Neutral));
            Assert.False(File.Exists(photo));
            Assert.NotEmpty(Directory.EnumerateFiles(dest, "*.jpg", SearchOption.AllDirectories));
        }
        finally
        {
            TryDelete(src);
            TryDelete(dest);
        }
    }

    [Fact]
    public async Task DeleteAfter_RejectedConfirm_SkipsCopy()
    {
        string src = Path.Combine(Path.GetTempPath(), "qmi-del-no-" + Path.GetRandomFileName());
        string dest = Path.Combine(Path.GetTempPath(), "qmi-del-no-dst-" + Path.GetRandomFileName());
        Directory.CreateDirectory(src);
        Directory.CreateDirectory(dest);
        string photo = Path.Combine(src, "shot.jpg");
        File.WriteAllBytes(photo, new byte[] { 8, 8, 8 });
        var prompt = new RecordingUserPrompt { NextConfirm = false };
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, new ImportTempPaths(), prompt);
        try
        {
            SelectOnly(host, src);
            host.SetDestination(dest);
            host.Model.DeleteAfterImport = true;
            Assert.False(await host.RunImportAsync(dryRun: false, IngestBenchCopy.Neutral));
            Assert.Single(prompt.Confirms);
            Assert.True(File.Exists(photo));
            Assert.Empty(Directory.EnumerateFiles(dest, "*.jpg", SearchOption.AllDirectories));
        }
        finally
        {
            TryDelete(src);
            TryDelete(dest);
        }
    }

    [Fact]
    public async Task DryRun_NotifiesViaPrompt()
    {
        string src = Path.Combine(Path.GetTempPath(), "qmi-dry-" + Path.GetRandomFileName());
        string dest = Path.Combine(Path.GetTempPath(), "qmi-dry-dst-" + Path.GetRandomFileName());
        Directory.CreateDirectory(src);
        Directory.CreateDirectory(dest);
        File.WriteAllBytes(Path.Combine(src, "shot.jpg"), new byte[] { 1 });
        var prompt = new RecordingUserPrompt();
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, new ImportTempPaths(), prompt);
        try
        {
            SelectOnly(host, src);
            host.SetDestination(dest);
            Assert.True(await host.RunImportAsync(dryRun: true, IngestBenchCopy.Neutral));
            Assert.NotEmpty(prompt.Notifies);
        }
        finally
        {
            TryDelete(src);
            TryDelete(dest);
        }
    }

    [Fact]
    public void CollectItems_FindsJpeg()
    {
        string src = Path.Combine(Path.GetTempPath(), "qmi-col-" + Path.GetRandomFileName());
        Directory.CreateDirectory(src);
        File.WriteAllBytes(Path.Combine(src, "a.jpg"), new byte[] { 1 });
        try
        {
            Assert.Single(IngestBenchImport.CollectItems(new[] { src }));
        }
        finally
        {
            TryDelete(src);
        }
    }

    [Fact]
    public void SplitBySource_SeparatesAdbSerialsAndLocal()
    {
        var group = new ItemGroup { Title = "Mix" };
        group.Items.Add(new ImportItem { FileName = "a.jpg", SourceId = "adb:AAA", IsSelected = true });
        group.Items.Add(new ImportItem { FileName = "b.jpg", SourceId = "adb:BBB", IsSelected = true });
        group.Items.Add(new ImportItem { FileName = "c.jpg", SourcePath = "/tmp/c.jpg", IsSelected = true });
        List<ItemGroup> parts = IngestBenchImportCopy.SplitBySource(group);
        Assert.Equal(3, parts.Count);
        Assert.Contains(parts, p => p.Items.Count == 1 && p.Items[0].FileName == "a.jpg");
        Assert.IsType<AdbFileProvider>(IngestBenchImportCopy.Create("adb:ABC123"));
        Assert.IsType<TrashingFileProvider>(IngestBenchImportCopy.Create("local"));
    }

    [Fact]
    public async Task RequestCancel_WithoutActiveImport_IsNoOp()
    {
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, new ImportTempPaths(), SilentUserPrompt.Instance);
        await IngestBenchImportCancel.RequestAsync(host, IngestBenchCopy.Neutral);
        Assert.Null(host.ImportCts);
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

file sealed class ImportTempPaths : IAppPaths
{
    public ImportTempPaths()
    {
        AppDataRoot = Path.Combine(Path.GetTempPath(), "qmi-imp-app-" + Path.GetRandomFileName());
        Directory.CreateDirectory(AppDataRoot);
    }

    public string AppDataRoot { get; }
}
