#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Prompt;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class OpenFolderSmokeTests
{
    [Fact]
    public async Task ImportOneJpeg_ThenOpenDestination()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("QMI_SMOKE_OPEN"), "1", StringComparison.Ordinal))
        {
            return;
        }

        string destRoot = PeekDest() ?? "/tmp/qmi-open-smoke-dest";
        destRoot = Path.Combine(destRoot, "_qmi-open-smoke");
        Directory.CreateDirectory(destRoot);
        string src = Path.Combine(Path.GetTempPath(), "qmi-open-src-" + Path.GetRandomFileName());
        Directory.CreateDirectory(src);
        File.WriteAllBytes(Path.Combine(src, "smoke.jpg"), new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 });
        var paths = new OpenSmokePaths();
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
            host.SetDestination(destRoot);
            host.Model.DeleteAfterImport = false;
            Assert.True(await host.RunImportAsync(dryRun: false, IngestBenchCopy.Neutral));
            Assert.NotEmpty(Directory.EnumerateFiles(destRoot, "*.jpg", SearchOption.AllDirectories));
            Assert.True(host.Model.ShowAfterglow);
            Assert.True(Directory.Exists(host.Model.AfterglowFolder));
            Assert.True(IngestBenchScene.OpenFolder(host.Model));
        }
        finally
        {
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

    private static string? PeekDest()
    {
        string destJson = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QuickMediaIngest",
            "dest.json");
        if (!File.Exists(destJson))
        {
            return null;
        }

        JsonNode? node = JsonNode.Parse(File.ReadAllText(destJson));
        string? saved = node?["DestinationRoot"]?.GetValue<string>();
        return string.IsNullOrWhiteSpace(saved) ? null : saved;
    }
}

file sealed class OpenSmokePaths : IAppPaths
{
    public OpenSmokePaths()
    {
        AppDataRoot = Path.Combine(Path.GetTempPath(), "qmi-open-app-" + Path.GetRandomFileName());
        Directory.CreateDirectory(AppDataRoot);
    }

    public string AppDataRoot { get; }
}
