#nullable enable
using System;
using QuickMediaIngest.Core.GitHubFeedback;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class GitHubIssueComposerTests
    {
        [Fact]
        public void BuildCrashTitle_IncludesFingerprintAndType()
        {
            Assert.Equal("[crash] a1b2c3d4e5f6 TypeError", GitHubIssueComposer.BuildCrashTitle("a1b2c3d4e5f6", "TypeError"));
        }

        [Fact]
        public void IsPlaceholderRepo_DetectsStubs()
        {
            Assert.True(GitHubIssueComposer.IsPlaceholderRepo("OWNER/REPO"));
            Assert.True(GitHubIssueComposer.IsPlaceholderRepo("acme/app"));
            Assert.True(GitHubIssueComposer.IsPlaceholderRepo(""));
            Assert.False(GitHubIssueComposer.IsPlaceholderRepo(GitHubIssueComposer.DefaultOwnerRepo));
        }

        [Fact]
        public void Compose_UsesHttpsOnlyAndDefaultRepo()
        {
            GitHubIssueLink link = GitHubIssueComposer.Compose("bug", "short description");
            Assert.StartsWith("https://github.com/edwardlthompson/QuickMediaIngest/issues/new", link.Url, StringComparison.Ordinal);
            Assert.False(link.UseClipboard);
            Assert.Contains("short description", link.SanitizedBody, StringComparison.Ordinal);
        }

        [Fact]
        public void Compose_LargeBody_UsesClipboardAndShortHttpsUrl()
        {
            string huge = new string('x', 1800);
            GitHubIssueLink link = GitHubIssueComposer.Compose("bug", huge);
            Assert.True(link.UseClipboard);
            Assert.Equal("https://github.com/edwardlthompson/QuickMediaIngest/issues/new", link.Url);
            Assert.Contains(huge, link.SanitizedBody, StringComparison.Ordinal);
        }

        [Fact]
        public void SearchDuplicates_FailSoftEmpty()
        {
            Assert.Empty(GitHubIssueComposer.SearchDuplicatesFailSoft());
        }
    }
}
