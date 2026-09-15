#nullable enable
using System;
using System.IO;
using System.Threading.Tasks;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Prompt;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class IngestBenchQueueTests
{
    [Fact]
    public void PendingPlan_PersistsAcrossHost()
    {
        string root = Path.Combine(Path.GetTempPath(), "qmi-q-" + Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        var paths = new QPaths(root);
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        IngestBenchQueue.SavePlan(host, new PendingImportPlan
        {
            DestinationRoot = "/tmp/out",
            NamingTemplate = "[Original]",
            SelectedSourcePaths = { "/tmp/a.jpg" },
        });
        Assert.True(host.Model.HasPendingImportPlan);
        Assert.True(host.Model.ShowResumePending);

        using IngestBenchHost again = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        Assert.True(again.Model.HasPendingImportPlan);
        IngestBenchQueue.ClearPlan(again);
        Assert.False(again.Model.HasPendingImportPlan);
    }

    [Fact]
    public async Task Enqueue_EmptySelection_IsNoOp()
    {
        var paths = new QPaths(Path.Combine(Path.GetTempPath(), "qmi-q0-" + Path.GetRandomFileName()));
        Directory.CreateDirectory(paths.AppDataRoot);
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        Assert.False(await IngestBenchQueue.EnqueueAsync(host, IngestBenchCopy.Neutral));
        Assert.Equal(0, host.Model.QueuedImportCount);
    }

    [Fact]
    public async Task Enqueue_WhileImporting_IncrementsQueue()
    {
        var paths = new QPaths(Path.Combine(Path.GetTempPath(), "qmi-q2-" + Path.GetRandomFileName()));
        Directory.CreateDirectory(paths.AppDataRoot);
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        host.Model.IsImporting = true;
        host.Groups.Add(new ItemGroup { Title = "s" });
        host.Groups[0].Items.Add(new ImportItem
        {
            SourcePath = "/tmp/x.jpg",
            FileName = "x.jpg",
            IsSelected = true,
        });
        Assert.True(await IngestBenchQueue.EnqueueAsync(host, IngestBenchCopy.Neutral));
        Assert.Equal(1, host.Model.QueuedImportCount);
        Assert.True(host.Model.IsImporting);
        Assert.Single(host.ImportJobs);
    }

    [Fact]
    public async Task RetryFailed_ImportsMarkedPaths()
    {
        string src = Path.Combine(Path.GetTempPath(), "qmi-retry-src-" + Path.GetRandomFileName());
        string dest = Path.Combine(Path.GetTempPath(), "qmi-retry-dst-" + Path.GetRandomFileName());
        Directory.CreateDirectory(src);
        Directory.CreateDirectory(dest);
        string photo = Path.Combine(src, "shot.jpg");
        File.WriteAllBytes(photo, new byte[] { 3, 3, 3 });
        var paths = new QPaths(Path.Combine(Path.GetTempPath(), "qmi-retry-app-" + Path.GetRandomFileName()));
        Directory.CreateDirectory(paths.AppDataRoot);
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
            host.SetDestination(dest);
            IngestBenchQueue.NoteFailed(host, new[] { Path.GetFullPath(photo) });
            Assert.True(host.Model.ShowRetryFailed);
            Assert.True(await IngestBenchQueue.RetryFailedAsync(host, IngestBenchCopy.Neutral));
            Assert.NotEmpty(Directory.EnumerateFiles(dest, "*.jpg", SearchOption.AllDirectories));
            Assert.False(host.Model.ShowRetryFailed);
        }
        finally
        {
            TryDelete(src);
            TryDelete(dest);
        }
    }

    [Fact]
    public async Task ResumePending_RestoresSelectionAndImports()
    {
        string src = Path.Combine(Path.GetTempPath(), "qmi-res-src-" + Path.GetRandomFileName());
        string dest = Path.Combine(Path.GetTempPath(), "qmi-res-dst-" + Path.GetRandomFileName());
        Directory.CreateDirectory(src);
        Directory.CreateDirectory(dest);
        File.WriteAllBytes(Path.Combine(src, "shot.jpg"), new byte[] { 4, 4, 4 });
        var paths = new QPaths(Path.Combine(Path.GetTempPath(), "qmi-res-app-" + Path.GetRandomFileName()));
        Directory.CreateDirectory(paths.AppDataRoot);
        try
        {
            using (IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance))
            {
                host.BeginDrivePick(new[] { src });
                string full = Path.GetFullPath(src);
                foreach (VolumeChoice volume in host.Model.Volumes)
                {
                    volume.IsSelected = string.Equals(volume.Path, full, StringComparison.Ordinal);
                }

                host.ConfirmDrivePick();
                host.SetDestination(dest);
                IngestBenchQueue.SavePlan(host, IngestBenchQueue.Snapshot(host));
            }

            using IngestBenchHost again = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
            Assert.True(again.Model.HasPendingImportPlan);
            Assert.True(await IngestBenchQueue.ResumeAsync(again, IngestBenchCopy.Neutral));
            Assert.NotEmpty(Directory.EnumerateFiles(dest, "*.jpg", SearchOption.AllDirectories));
            Assert.False(again.Model.HasPendingImportPlan);
        }
        finally
        {
            TryDelete(src);
            TryDelete(dest);
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

    private sealed class QPaths : IAppPaths
    {
        public QPaths(string root) => AppDataRoot = root;

        public string AppDataRoot { get; }
    }
}
