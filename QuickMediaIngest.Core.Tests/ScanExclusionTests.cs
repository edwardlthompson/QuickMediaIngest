#nullable enable
using System.IO;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Prompt;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class ScanExclusionTests
{
    [Fact]
    public void Matcher_PrefixIsSlashInsensitive()
    {
        Assert.True(ScanExclusionMatcher.IsUnder("/tmp/card/DCIM", new[] { "/tmp/card/DCIM/" }));
        Assert.False(ScanExclusionMatcher.IsUnder("/tmp/card", new[] { "/tmp/card/DCIM" }));
    }

    [Fact]
    public void Store_RoundTripsAndHostAddRemove()
    {
        string root = Path.Combine(Path.GetTempPath(), "qmi-excl-" + Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        var paths = new ExclPaths(root);
        IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        IngestBenchExclusions.Add(host, Path.Combine(root, "DCIM") + Path.DirectorySeparatorChar);
        Assert.Single(host.Model.ExcludedFolders);
        IngestBenchHost again = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        Assert.Single(again.Model.ExcludedFolders);
        IngestBenchExclusions.Remove(again, again.Model.ExcludedFolders[0]);
        Assert.Empty(again.Model.ExcludedFolders);
        IngestBenchHost third = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        Assert.Empty(third.Model.ExcludedFolders);
    }

    private sealed class ExclPaths : IAppPaths
    {
        public ExclPaths(string root) => AppDataRoot = root;

        public string AppDataRoot { get; }
    }
}
