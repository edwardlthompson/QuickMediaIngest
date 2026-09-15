#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Prompt;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class DualAdbImportSmokeTests
{
    [Fact]
    public async Task ImportsOneJpegFromEachConnectedDevice()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("QMI_SMOKE_ADB"), "1", StringComparison.Ordinal))
        {
            return;
        }

        string[] serials = AdbDeviceProbe.ListDeviceSerials().ToArray();
        Assert.True(serials.Length >= 2, "Need two ADB devices; got " + serials.Length);

        var destJson = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QuickMediaIngest",
            "dest.json");
        string destRoot = "/tmp/qmi-adb-smoke";
        if (File.Exists(destJson))
        {
            JsonNode? node = JsonNode.Parse(await File.ReadAllTextAsync(destJson));
            string? saved = node?["DestinationRoot"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(saved))
            {
                destRoot = saved;
            }
        }

        destRoot = Path.Combine(destRoot, "_qmi-smoke");
        Directory.CreateDirectory(destRoot);
        var paths = new SmokePaths();
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        host.SetDestination(destRoot);
        host.Model.DeleteAfterImport = false;
        host.Model.Prefs.ConfirmBeforeImport = false;

        var clock = Stopwatch.StartNew();
        IngestBenchAdb.AdbScanResult scan = IngestBenchAdb.Collect();
        clock.Stop();
        IngestBenchAdb.ApplyResult(host, scan);
        Console.WriteLine(
            "ADB scan {0} ms: {1} files from {2} device(s). {3}",
            clock.ElapsedMilliseconds,
            scan.Items.Count,
            scan.DeviceCount,
            scan.Status);
        foreach (var per in scan.Items.GroupBy(i => i.SourceId, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine("  " + per.Key + ": " + per.Count() + " files");
        }

        Assert.True(scan.Items.Count > 0, scan.Status);

        foreach (ImportItem item in host.Groups.SelectMany(g => g.Items))
        {
            item.IsSelected = false;
        }

        var picked = scan.Items
            .Where(i => i.SourceId.StartsWith("adb:", StringComparison.OrdinalIgnoreCase))
            .GroupBy(i => i.SourceId, StringComparer.OrdinalIgnoreCase)
            .Select(g => g
                .Where(i => i.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                    || i.FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                .OrderBy(i => i.FileSize <= 0 ? long.MaxValue : i.FileSize)
                .FirstOrDefault()
                ?? g.OrderBy(i => i.FileSize <= 0 ? long.MaxValue : i.FileSize).First())
            .ToList();
        Assert.True(picked.Count >= 2, "Need one file from each device; scan had " + scan.Items.Count);
        foreach (ImportItem item in picked.Take(2))
        {
            item.IsSelected = true;
            Console.WriteLine("Picked " + item.SourceId + " " + item.FileName + " " + item.FileSize + " bytes " + item.SourcePath);
        }

        Assert.True(await host.RunImportAsync(dryRun: false, IngestBenchCopy.Neutral));
        Assert.Empty(host.FailedPaths);
        foreach (ImportItem item in picked.Take(2))
        {
            bool found = Directory.EnumerateFiles(destRoot, "*", SearchOption.AllDirectories)
                .Any(p => Path.GetFileName(p).Contains(
                    Path.GetFileNameWithoutExtension(item.FileName),
                    StringComparison.OrdinalIgnoreCase));
            Assert.True(found, "Missing imported file for " + item.SourceId + " " + item.FileName);
        }

        _ = clock.Elapsed;
    }
}

file sealed class SmokePaths : IAppPaths
{
    public SmokePaths()
    {
        AppDataRoot = Path.Combine(Path.GetTempPath(), "qmi-adb-smoke-" + Path.GetRandomFileName());
        Directory.CreateDirectory(AppDataRoot);
    }

    public string AppDataRoot { get; }
}
