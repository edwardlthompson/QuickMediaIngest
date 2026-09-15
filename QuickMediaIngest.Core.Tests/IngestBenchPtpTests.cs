#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Prompt;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class IngestBenchPtpTests
{
    [Fact]
    public void ListRoots_DoesNotThrow()
    {
        IReadOnlyList<string> roots = IngestBenchPtp.ListRoots();
        Assert.NotNull(roots);
    }

    [Fact]
    public void Open_ShowsPicker()
    {
        string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "qmi-ptp-" + System.IO.Path.GetRandomFileName());
        System.IO.Directory.CreateDirectory(root);
        var paths = new PtpPaths(root);
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        IngestBenchPtp.Open(host);
        Assert.True(host.Model.ShowPtpPicker);
        IngestBenchPtp.Close(host);
        Assert.False(host.Model.ShowPtpPicker);
    }

    [Fact]
    public async Task ScanPtpDevice_EmptyId_ReturnsEmpty()
    {
        var scanner = new PtpTetherScanner();
        Assert.True(scanner.IsSupportedOnPlatform);
        Assert.Empty(await scanner.ScanPtpDeviceAsync("mock-ptp-1"));
        Assert.Empty(await IngestBenchPtp.ScanAsync("mock-ptp-1"));
    }
}

file sealed class PtpPaths : IAppPaths
{
    public PtpPaths(string root) => AppDataRoot = root;

    public string AppDataRoot { get; }
}
