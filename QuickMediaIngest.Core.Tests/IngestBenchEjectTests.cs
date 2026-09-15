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

[CollectionDefinition("eject-statics", DisableParallelization = true)]
public sealed class EjectStaticsCollection
{
}

[Collection("eject-statics")]
public sealed class IngestBenchEjectTests
{
    [Fact]
    public void SafetyAndCommands_MatchLinuxRemovableRoots()
    {
        Assert.False(IngestBenchEject.IsSafeUnmountTarget("/"));
        Assert.False(IngestBenchEject.IsSafeUnmountTarget("/home"));
        Assert.True(IngestBenchEject.IsSafeUnmountTarget("/media/sd"));
        Assert.True(IngestBenchEject.IsSafeUnmountTarget("/run/media/ed/CARD"));
        Assert.True(IngestBenchEject.IsSafeUnmountTarget("/mnt/card"));
        IReadOnlyList<(string File, string[] Args)> cmds = IngestBenchEject.BuildUnmountCommands("/media/sd");
        Assert.Equal("gio", cmds[0].File);
        Assert.Equal(new[] { "mount", "-u", "/media/sd" }, cmds[0].Args);
        Assert.Equal("udisksctl", cmds[1].File);
        Assert.Equal(new[] { "unmount", "-p", "/media/sd" }, cmds[1].Args);
    }

    [Fact]
    public void Leftover_RequiresDeleteAfterAndExistingLocalFile()
    {
        string path = Path.Combine(Path.GetTempPath(), "qmi-eject-" + Path.GetRandomFileName() + ".jpg");
        File.WriteAllBytes(path, new byte[] { 1, 2 });
        var groups = new[]
        {
            new ItemGroup
            {
                Items = { new ImportItem { SourcePath = path, IsFtpSource = false } },
            },
        };
        try
        {
            Assert.False(IngestBenchEject.HasLeftoverLocalFiles(groups, deleteAfterImport: false));
            Assert.True(IngestBenchEject.HasLeftoverLocalFiles(groups, deleteAfterImport: true));
            File.Delete(path);
            Assert.False(IngestBenchEject.HasLeftoverLocalFiles(groups, deleteAfterImport: true));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task AfterImport_LeftoverNotifiesAndSkipsUnmount()
    {
        string root = Path.Combine(Path.GetTempPath(), "qmi-eject-app-" + Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        string leftover = Path.Combine(root, "keep.jpg");
        File.WriteAllBytes(leftover, new byte[] { 3 });
        var prompt = new RecordingUserPrompt();
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, new EjectPaths(root), prompt);
        host.Model.Prefs.EjectAfterImport = true;
        host.Model.DeleteAfterImport = true;
        var groups = new List<ItemGroup>
        {
            new() { Items = { new ImportItem { SourcePath = leftover, IsFtpSource = false } } },
        };
        var ran = new List<string>();
        IngestBenchEject.RunProcess = (file, args) =>
        {
            ran.Add(file);
            return 0;
        };
        IngestBenchEject.MountPointResolver = _ => "/media/qmi-test";
        try
        {
            await IngestBenchEject.AfterImportAsync(host, groups, IngestBenchCopy.Neutral);
            Assert.Single(prompt.Notifies);
            Assert.Equal("Msg_EjectLeftover_Title", prompt.Notifies[0].Title);
            Assert.Empty(ran);
        }
        finally
        {
            IngestBenchEject.RunProcess = null;
            IngestBenchEject.MountPointResolver = null;
        }
    }

    [Fact]
    public async Task AfterImport_RunsGioThenUdisksWhenPrefOn()
    {
        string root = Path.Combine(Path.GetTempPath(), "qmi-eject-ok-" + Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        var prompt = new RecordingUserPrompt();
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, new EjectPaths(root), prompt);
        host.BeginDrivePick(new[] { root });
        foreach (VolumeChoice volume in host.Model.Volumes)
        {
            volume.IsSelected = true;
        }

        host.ConfirmDrivePick();
        host.Model.Prefs.EjectAfterImport = true;
        host.Model.DeleteAfterImport = false;
        var ran = new List<(string File, string Args)>();
        IngestBenchEject.MountPointResolver = _ => "/media/qmi-test";
        IngestBenchEject.RunProcess = (file, args) =>
        {
            ran.Add((file, string.Join(' ', args)));
            return file == "gio" ? 1 : 0;
        };
        try
        {
            await IngestBenchEject.AfterImportAsync(host, Array.Empty<ItemGroup>(), IngestBenchCopy.Neutral);
            Assert.Equal(2, ran.Count);
            Assert.Equal("gio", ran[0].File);
            Assert.Contains("mount -u /media/qmi-test", ran[0].Args);
            Assert.Equal("udisksctl", ran[1].File);
            Assert.Empty(prompt.Notifies);
        }
        finally
        {
            IngestBenchEject.RunProcess = null;
            IngestBenchEject.MountPointResolver = null;
        }
    }

    [Fact]
    public async Task EjectNow_NotifiesWhenImporting()
    {
        string root = Path.Combine(Path.GetTempPath(), "qmi-eject-busy-" + Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        var prompt = new RecordingUserPrompt();
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, new EjectPaths(root), prompt);
        host.Model.IsImporting = true;
        await IngestBenchEject.EjectNowAsync(host, Array.Empty<ItemGroup>(), IngestBenchCopy.Neutral);
        Assert.Equal("Msg_EjectBusy_Title", prompt.Notifies[0].Title);
    }
}

file sealed class EjectPaths : IAppPaths
{
    public EjectPaths(string root) => AppDataRoot = root;

    public string AppDataRoot { get; }
}
