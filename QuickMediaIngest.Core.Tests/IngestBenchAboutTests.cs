#nullable enable
using System.IO;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Nav;
using QuickMediaIngest.Core.Prompt;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class IngestBenchAboutTests
{
    [Fact]
    public void DebPackage_MatchesVersionedDebFilename()
    {
        Assert.True(ReleaseAssetVersion.MatchesPackage("quick-media-ingest_1.4.0_amd64.deb", "Deb"));
        Assert.False(ReleaseAssetVersion.MatchesPackage("QuickMediaIngest-1.4.0-x64.exe", "Deb"));
        Assert.Equal("1.4.0", ReleaseAssetVersion.TryParse("quick-media-ingest_1.4.0_amd64.deb")?.ToString(3));
    }

    [Fact]
    public void DonateLink_IsHttpsVenmo()
    {
        Assert.StartsWith("https://venmo.com/", DonationLinks.Venmo);
    }

    [Fact]
    public void Open_SetsVersion_AndRecordsInstallOnce()
    {
        string root = Path.Combine(Path.GetTempPath(), "qmi-about-" + Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        var paths = new AboutPaths(root);
        IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        IngestBenchAbout.Open(host);
        Assert.True(host.Model.Nav.IsVisible(OverlayId.About));
        Assert.False(string.IsNullOrWhiteSpace(host.Model.AboutVersion));
        var store = new FileUpdateDonateStore(root);
        Assert.Equal(IngestBenchAbout.CurrentVersion, store.Load().RecordedInstalledVersion);
    }

    private sealed class AboutPaths : IAppPaths
    {
        public AboutPaths(string root) => AppDataRoot = root;

        public string AppDataRoot { get; }
    }
}
