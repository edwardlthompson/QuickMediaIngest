#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuickMediaIngest.Core.GitHubFeedback;
using QuickMediaIngest.Core.Nav;
using QuickMediaIngest.Core.PrivacyReport;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Feedback overlay: preview, fail-soft GitHub search, compose https issue URL.</summary>
    public static class IngestBenchFeedback
    {
        public static void Open(IngestBenchHost host, string kind, IngestBenchCopy copy)
        {
            IngestBenchAppModel model = host.Model;
            model.FeedbackKind = kind == "feature" ? "feature" : "bug";
            model.FeedbackTitle = model.FeedbackKind == "feature"
                ? copy.Get("Feedback_FeatureTitle")
                : copy.Get("Feedback_BugTitle");
            host.Model.Nav.Push(OverlayId.Feedback);
            Refresh(host, copy);
        }

        public static void Refresh(IngestBenchHost host, IngestBenchCopy copy)
        {
            IngestBenchAppModel model = host.Model;
            model.FeedbackPreview = PrivacyReportMarkdown.BuildReportMarkdown(
                model.FeedbackKind,
                model.FeedbackDescription,
                osFamily: OperatingSystem.IsLinux() ? "Linux" : "Windows");
            bool hasBody = !string.IsNullOrWhiteSpace(model.FeedbackDescription);
            model.FeedbackCanOpenGitHub = hasBody;
            model.FeedbackGitHubReason = hasBody ? string.Empty : copy.Get("Feedback_NeedDescription");
        }

        public static GitHubIssueLink Compose(IngestBenchHost host) =>
            GitHubIssueComposer.Compose(host.Model.FeedbackKind, host.Model.FeedbackDescription);

        public static bool TryOpenGitHub(IngestBenchHost host)
        {
            if (!host.Model.FeedbackCanOpenGitHub)
            {
                return false;
            }

            GitHubIssueLink link = Compose(host);
            return ShellOpen.TryOpen(link.Url);
        }

        public static void Discard(IngestBenchHost host)
        {
            host.Model.FeedbackDescription = string.Empty;
            host.Model.FeedbackPreview = string.Empty;
            host.Model.FeedbackDuplicates.Clear();
            host.Model.Nav.Pop();
        }

        public static async Task SearchAsync(IngestBenchHost host)
        {
            IReadOnlyList<string> hits = await GitHubIssueComposer.SearchDuplicatesFailSoftAsync(
                host.Model.FeedbackDescription);
            host.Model.FeedbackDuplicates.Clear();
            foreach (string hit in hits)
            {
                host.Model.FeedbackDuplicates.Add(hit);
            }
        }
    }
}
