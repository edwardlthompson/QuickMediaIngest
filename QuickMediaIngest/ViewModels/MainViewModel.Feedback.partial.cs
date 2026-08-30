#nullable enable
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickMediaIngest.Core.CrashCapture;
using QuickMediaIngest.Core.GitHubFeedback;
using QuickMediaIngest.Core.PrivacyReport;
using QuickMediaIngest.Localization;

namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel
    {
        [ObservableProperty] private bool showFeedbackDialog;
        [ObservableProperty] private string feedbackKind = "bug";
        [ObservableProperty] private string feedbackTitle = string.Empty;
        [ObservableProperty] private string feedbackDescription = string.Empty;
        [ObservableProperty] private string feedbackPreview = string.Empty;
        [ObservableProperty] private string feedbackGitHubReason = string.Empty;
        [ObservableProperty] private bool feedbackCanOpenGitHub;
        [ObservableProperty] private bool saveCrashDetails;

        partial void OnFeedbackDescriptionChanged(string value) => RefreshFeedbackPreview();

        partial void OnSaveCrashDetailsChanged(bool value)
        {
            if (!_loadingConfig)
            {
                new CrashCaptureService(new FilePendingCrashStore(), () => value).ApplyOptIn(value);
                SaveConfig();
            }
        }

        [RelayCommand]
        private void ReportBug() => OpenFeedback("bug");

        [RelayCommand]
        private void RequestFeature() => OpenFeedback("feature");

        [RelayCommand]
        private void CopyFeedback()
        {
            try
            {
                Clipboard.SetText(FeedbackPreview ?? string.Empty);
            }
            catch
            {
                // Clipboard can fail in remote sessions; preview remains on screen.
            }
        }

        [RelayCommand]
        private void OpenFeedbackGitHub()
        {
            if (!FeedbackCanOpenGitHub)
            {
                return;
            }

            GitHubIssueLink link = BuildFeedbackLink();
            if (link.UseClipboard)
            {
                CopyFeedback();
            }

            _shellService.OpenUrl(link.Url);
        }

        [RelayCommand]
        private void DiscardFeedback()
        {
            FeedbackDescription = string.Empty;
            ShowFeedbackDialog = false;
            new FilePendingCrashStore().Clear();
        }

        public void OfferPendingCrashReview()
        {
            PendingCrash? pending = new FilePendingCrashStore().Load();
            if (pending is null || !SaveCrashDetails)
            {
                return;
            }

            FeedbackKind = "crash";
            FeedbackTitle = AppLocalizer.Get("Feedback_CrashTitle");
            FeedbackDescription = pending.Description;
            ShowAboutDialog = false;
            ShowFeedbackDialog = true;
            RefreshFeedbackPreview();
        }

        private void OpenFeedback(string kind)
        {
            FeedbackKind = kind;
            FeedbackTitle = kind == "feature"
                ? AppLocalizer.Get("Feedback_FeatureTitle")
                : AppLocalizer.Get("Feedback_BugTitle");
            ShowAboutDialog = false;
            ShowFeedbackDialog = true;
            RefreshFeedbackPreview();
        }

        private void RefreshFeedbackPreview()
        {
            PendingCrash? pending = FeedbackKind == "crash" ? new FilePendingCrashStore().Load() : null;
            FeedbackPreview = PrivacyReportMarkdown.BuildReportMarkdown(
                FeedbackKind,
                FeedbackDescription,
                pending?.Stack,
                pending?.ExceptionType,
                pending?.Fingerprint,
                AppVersion);
            bool hasBody = !string.IsNullOrWhiteSpace(FeedbackDescription) || !string.IsNullOrWhiteSpace(pending?.Stack);
            FeedbackCanOpenGitHub = hasBody;
            FeedbackGitHubReason = hasBody
                ? string.Empty
                : AppLocalizer.Get("Feedback_NeedDescription");
        }

        private GitHubIssueLink BuildFeedbackLink()
        {
            PendingCrash? pending = FeedbackKind == "crash" ? new FilePendingCrashStore().Load() : null;
            return GitHubIssueComposer.Compose(
                FeedbackKind,
                FeedbackDescription,
                pending?.Stack,
                pending?.ExceptionType,
                pending?.Fingerprint,
                AppVersion);
        }
    }
}
