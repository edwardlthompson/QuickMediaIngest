#nullable enable
using System;
using System.Collections.Generic;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Prompt;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class IngestBenchAdbTests
{
    [Fact]
    public void Apply_DoesNotThrow_AndDedupesAliasesWhenPreferAdb()
    {
        var paths = new AdbTempPaths();
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        IngestBenchAdb.Apply(host);
        host.Model.PreferAdb = true;
        var stamp = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        host.Groups.Add(new ItemGroup
        {
            Title = "Shoot 1",
            Items =
            {
                new ImportItem { FileName = "DSC_0001.JPG", FileSize = 10, DateTaken = stamp, IsFtpSource = true },
                new ImportItem { FileName = "DSC_0001.JPG", FileSize = 10, DateTaken = stamp, SourceId = "adb:1" },
            },
        });
        IngestBenchAdb.DedupeGroups(host);
        Assert.Single(host.Groups[0].Items);
        Assert.Single(FtpAdbAliasFilter.DeduplicateDualFtpAliases(new List<ImportItem>
        {
            new() { FileName = "a.jpg", FileSize = 1, DateTaken = stamp },
            new() { FileName = "a.jpg", FileSize = 1, DateTaken = stamp },
        }));
    }

    [Fact]
    public void TryResolveForSerial_UsesMatchingRoot()
    {
        var probe = new StubPathProbe("/sdcard/DCIM");
        AdbTransferSession? session = AdbTransferEligibility.TryResolveForSerial("ABC123", "/DCIM", probe);
        Assert.NotNull(session);
        Assert.Equal("ABC123", session.Value.DeviceSerial);
        Assert.Equal("/sdcard", session.Value.MediaRootPrefix);
        Assert.Null(AdbTransferEligibility.TryResolveForSerial(" ", "/DCIM", probe));
    }

    [Fact]
    public void PicturesVolumePath_ParsesSerial()
    {
        string path = IngestBenchAdb.PicturesVolumePath("ABC123");
        Assert.Equal("adb:ABC123/pictures", path);
        Assert.True(IngestBenchAdb.IsPicturesVolume(path));
        Assert.False(IngestBenchAdb.IsPicturesVolume("adb:ABC123"));
        Assert.Equal("ABC123", IngestBenchAdb.SerialOf(path));
        Assert.Equal("ABC123", IngestBenchAdb.SerialOf("adb:ABC123"));
    }

    [Fact]
    public void ApplyResult_Empty_SetsStatusAndClearsVolumes()
    {
        var paths = new AdbTempPaths();
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        host.Model.Volumes.Add(new VolumeChoice("/tmp", "tmp", "Local"));
        IngestBenchAdb.ApplyResult(
            host,
            new IngestBenchAdb.AdbScanResult(
                0,
                Array.Empty<ImportItem>(),
                Array.Empty<VolumeChoice>(),
                "No ADB devices."));
        Assert.Empty(host.Model.Volumes);
        Assert.Equal("No ADB devices.", host.Model.ImportStatus);
        Assert.Equal(0, host.Model.SourceCount);
    }
}

file sealed class AdbTempPaths : IAppPaths
{
    public AdbTempPaths()
    {
        AppDataRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "qmi-adb-" + System.IO.Path.GetRandomFileName());
        System.IO.Directory.CreateDirectory(AppDataRoot);
    }

    public string AppDataRoot { get; }
}

file sealed class StubPathProbe : IAdbPathProbe
{
    private readonly string _okPath;

    public StubPathProbe(string okPath) => _okPath = okPath;

    public bool DirectoryExists(string deviceSerial, string remoteDirectory) =>
        string.Equals(remoteDirectory, _okPath, StringComparison.OrdinalIgnoreCase);

    public bool FileExists(string deviceSerial, string remoteFilePath) =>
        remoteFilePath.StartsWith(_okPath, StringComparison.OrdinalIgnoreCase);
}
