#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Prompt;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class IngestBenchFtpTests
{
    [Fact]
    public void Save_PersistsSourceAndPassword()
    {
        var paths = new FtpTempPaths();
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        host.Model.Ftp.Host = "ftp.example.com";
        host.Model.Ftp.User = "cam";
        host.Model.Ftp.Password = "secret";
        host.Model.Ftp.RemoteFolder = "DCIM";
        host.Model.Ftp.ThrottleKbpsText = "128";
        IngestBenchFtp.Save(host);
        Assert.False(host.Model.ShowFtpEditor);
        Assert.Contains(host.Model.FtpSources, s => s.Host == "ftp.example.com" && s.RemoteFolder == "/DCIM");
        Assert.True(IngestBenchFtp.Store(paths).TryReadPassword("ftp.example.com", 21, out string password));
        Assert.Equal("secret", password);
        Assert.Equal(128 * 1024, IngestBenchFtp.CreateThrottler(host.Model.Ftp).BytesPerSecondLimit);

        using IngestBenchHost again = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        Assert.Contains(again.Model.FtpSources, s => s.Host == "ftp.example.com");
    }

    [Fact]
    public async Task TestAndBrowse_UseScanner()
    {
        var paths = new FtpTempPaths();
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        host.Model.Ftp.Host = "ftp.example.com";
        var scanner = new FakeFtpScanner();
        await IngestBenchFtp.TestAsync(host, scanner);
        Assert.Equal("ok", host.Model.Ftp.Status);
        await IngestBenchFtp.BrowseAsync(host, scanner);
        Assert.Equal("DCIM", host.Model.Ftp.BrowseListing);
        IngestBenchFtp.Open(host);
        Assert.True(host.Model.ShowFtpEditor);
        IngestBenchFtp.OpenThrottle(host);
        Assert.True(host.Model.ShowFtpThrottle);
        IngestBenchFtp.Close(host);
        Assert.False(host.Model.ShowFtpEditor);
    }

    [Fact]
    public void Save_RequiresHost()
    {
        var paths = new FtpTempPaths();
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        IngestBenchFtp.Open(host);
        IngestBenchFtp.Save(host);
        Assert.Equal("Host required", host.Model.Ftp.Status);
        Assert.True(host.Model.ShowFtpEditor);
    }

    [Fact]
    public void ApplyWifiPreset_SetsSonyFolder()
    {
        var paths = new FtpTempPaths();
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        host.Model.Ftp.WifiBrand = "Sony";
        IngestBenchFtp.ApplyWifiPreset(host);
        Assert.Equal("/DCIM/100MSDCF", host.Model.Ftp.RemoteFolder);
        Assert.Contains("Sony", host.Model.Ftp.Status);
    }

    [Fact]
    public async Task Test_Failure_SetsFtpFailurePanelCopy()
    {
        var paths = new FtpTempPaths();
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        host.Model.Ftp.Host = "ftp.example.com";
        await IngestBenchFtp.TestAsync(host, new FailFtpScanner());
        Assert.True(host.Model.ShowFtpFailure);
        Assert.Contains("/DCIM", host.Model.Ftp.Status);
        Assert.Equal("down", host.Model.Ftp.BrowseListing);
        IngestBenchFtp.DismissFailure(host);
        Assert.False(host.Model.ShowFtpFailure);
    }
}

file sealed class FailFtpScanner : IFtpScanner
{
    public Task<List<string>> ListDirectoriesAsync(
        string host, int port, string user, string pass, string remotePath,
        int timeoutSeconds = 15, CancellationToken cancellationToken = default) =>
        Task.FromResult(new List<string>());

    public Task<(bool Success, string Message)> TestConnectionAsync(
        string host, int port, string user, string pass, string remotePath,
        int timeoutSeconds = 15, CancellationToken cancellationToken = default) =>
        Task.FromResult((false, "down"));

    public Task<List<ImportItem>> ScanAsync(
        string host, int port, string user, string pass, string remotePath, bool includeSubfolders,
        int timeoutSeconds = 20, CancellationToken cancellationToken = default,
        System.Action<FtpScanProgress>? progressCallback = null) =>
        Task.FromResult(new List<ImportItem>());
}

file sealed class FakeFtpScanner : IFtpScanner
{
    public Task<List<string>> ListDirectoriesAsync(
        string host, int port, string user, string pass, string remotePath,
        int timeoutSeconds = 15, CancellationToken cancellationToken = default) =>
        Task.FromResult(new List<string> { "DCIM" });

    public Task<(bool Success, string Message)> TestConnectionAsync(
        string host, int port, string user, string pass, string remotePath,
        int timeoutSeconds = 15, CancellationToken cancellationToken = default) =>
        Task.FromResult((true, "ok"));

    public Task<List<ImportItem>> ScanAsync(
        string host, int port, string user, string pass, string remotePath, bool includeSubfolders,
        int timeoutSeconds = 20, CancellationToken cancellationToken = default,
        System.Action<FtpScanProgress>? progressCallback = null) =>
        Task.FromResult(new List<ImportItem>());
}

file sealed class FtpTempPaths : IAppPaths
{
    public FtpTempPaths()
    {
        AppDataRoot = Path.Combine(Path.GetTempPath(), "qmi-ftp-app-" + Path.GetRandomFileName());
        Directory.CreateDirectory(AppDataRoot);
    }

    public string AppDataRoot { get; }
}
