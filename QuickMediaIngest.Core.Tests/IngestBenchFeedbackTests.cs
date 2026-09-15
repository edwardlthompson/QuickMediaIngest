#nullable enable
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Nav;
using QuickMediaIngest.Core.Prompt;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class IngestBenchFeedbackTests
{
    [Fact]
    public void Open_RequiresDescription_ForGitHub()
    {
        var paths = new FbPaths();
        IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        IngestBenchFeedback.Open(host, "bug", IngestBenchCopy.Neutral);
        Assert.True(host.Model.Nav.IsVisible(OverlayId.Feedback));
        Assert.False(host.Model.FeedbackCanOpenGitHub);
        host.Model.FeedbackDescription = "cannot copy from SD";
        IngestBenchFeedback.Refresh(host, IngestBenchCopy.Neutral);
        Assert.True(host.Model.FeedbackCanOpenGitHub);
        Assert.Contains("cannot copy", host.Model.FeedbackPreview);
        var link = IngestBenchFeedback.Compose(host);
        Assert.StartsWith("https://github.com/", link.Url);
        IngestBenchFeedback.Discard(host);
        Assert.False(host.Model.Nav.IsVisible(OverlayId.Feedback));
        Assert.Equal(string.Empty, host.Model.FeedbackDescription);
    }

    [Fact]
    public async System.Threading.Tasks.Task Search_EmptyQuery_IsEmpty()
    {
        var paths = new FbPaths();
        IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        host.Model.FeedbackDescription = "   ";
        await IngestBenchFeedback.SearchAsync(host);
        Assert.Empty(host.Model.FeedbackDuplicates);
    }

    private sealed class FbPaths : QuickMediaIngest.Core.IAppPaths
    {
        public FbPaths()
        {
            AppDataRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "qmi-fb-" + System.IO.Path.GetRandomFileName());
            System.IO.Directory.CreateDirectory(AppDataRoot);
        }

        public string AppDataRoot { get; }
    }
}
