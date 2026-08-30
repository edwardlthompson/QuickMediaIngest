#nullable enable
using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using QuickMediaIngest;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.CrashCapture;
using QuickMediaIngest.Core.Services;
using QuickMediaIngest.Data;
using QuickMediaIngest.Services;
using QuickMediaIngest.ViewModels;
using Xunit;

namespace QuickMediaIngest.Tests
{
    [Collection("Wpf")]
    public class MainViewModelFeedbackTests
    {
        private static string ConfigPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest", "config.json");

        [Fact]
        public void ReportBug_OpensBugFeedbackDialog()
        {
            WpfTestHost.EnsureInitialized();
            MainViewModel vm = MainViewModelProofTests.CreateViewModel();

            vm.ReportBugCommand.Execute(null);

            Assert.True(vm.ShowFeedbackDialog);
            Assert.False(vm.ShowAboutDialog);
            Assert.Equal("bug", vm.FeedbackKind);
            Assert.False(vm.FeedbackCanOpenGitHub);
            Assert.False(string.IsNullOrWhiteSpace(vm.FeedbackGitHubReason));
        }

        [Fact]
        public void RequestFeature_OpensFeatureFeedbackDialog()
        {
            WpfTestHost.EnsureInitialized();
            MainViewModel vm = MainViewModelProofTests.CreateViewModel();

            vm.RequestFeatureCommand.Execute(null);

            Assert.True(vm.ShowFeedbackDialog);
            Assert.False(vm.ShowAboutDialog);
            Assert.Equal("feature", vm.FeedbackKind);
            Assert.False(vm.FeedbackCanOpenGitHub);
        }

        [Fact]
        public void EnteringDescription_EnablesOpenGitHub()
        {
            WpfTestHost.EnsureInitialized();
            MainViewModel vm = MainViewModelProofTests.CreateViewModel();
            vm.ReportBugCommand.Execute(null);

            vm.FeedbackDescription = "Issue repro details";

            Assert.True(vm.FeedbackCanOpenGitHub);
            Assert.Equal(string.Empty, vm.FeedbackGitHubReason);
            Assert.Contains("Issue repro details", vm.FeedbackPreview);
        }

        [Fact]
        public void OpenFeedbackGitHub_InvokesShellWithHttpsUrl()
        {
            WpfTestHost.EnsureInitialized();
            var shellMock = new Mock<IShellService>();
            string? openedUrl = null;
            shellMock.Setup(s => s.OpenUrl(It.IsAny<string>()))
                     .Callback<string>(url => openedUrl = url);

            MainViewModel vm = new MainViewModel(
                new Mock<ILocalScanner>().Object,
                new Mock<IFtpScanner>().Object,
                new Mock<IThumbnailService>().Object,
                new Mock<IUpdateService>().Object,
                new Mock<IUpdateDonateStore>().Object,
                new Mock<IDeviceWatcher>().Object,
                new Mock<IFileProviderFactory>().Object,
                new Mock<IIngestEngineFactory>().Object,
                new GroupBuilder(),
                new Mock<IDatabaseService>().Object,
                new Mock<IShootFilterService>().Object,
                new Mock<IFtpWorkflowService>().Object,
                new Mock<IUnifiedConcreteSourceScanService>().Object,
                new Mock<IFtpCredentialStore>().Object,
                new Mock<IFtpThumbnailService>().Object,
                new Mock<IAdbMediaScanner>().Object,
                new Mock<IAdbPreviewFetcher>().Object,
                new Mock<IAdbVideoThumbnailFetcher>().Object,
                new Mock<IAdbPathProbe>().Object,
                new Mock<IFileDialogService>().Object,
                shellMock.Object,
                new Mock<Microsoft.Extensions.Logging.ILogger<MainViewModel>>().Object);

            vm.ReportBugCommand.Execute(null);
            vm.FeedbackDescription = "Bug details";

            vm.OpenFeedbackGitHubCommand.Execute(null);

            Assert.NotNull(openedUrl);
            Assert.StartsWith("https://github.com/", openedUrl);
        }

        [Fact]
        public void DiscardFeedback_ClearsStateAndStore()
        {
            WpfTestHost.EnsureInitialized();
            var store = new FilePendingCrashStore();
            store.Replace(new PendingCrash
            {
                Fingerprint = "test123",
                ExceptionType = "TestException",
                Description = "A crash",
                Stack = "at Main()",
                AppVersion = "1.3.27"
            });

            MainViewModel vm = MainViewModelProofTests.CreateViewModel();
            vm.SaveCrashDetails = true;
            vm.OfferPendingCrashReview();
            Assert.True(vm.ShowFeedbackDialog);

            vm.DiscardFeedbackCommand.Execute(null);

            Assert.False(vm.ShowFeedbackDialog);
            Assert.Equal(string.Empty, vm.FeedbackDescription);
            Assert.Null(store.Load());
        }

        [Fact]
        public void DiscardFeedback_ClearsClipboardIfWeWroteIt()
        {
            WpfTestHost.EnsureInitialized();
            MainViewModel vm = MainViewModelProofTests.CreateViewModel();
            vm.ReportBugCommand.Execute(null);
            vm.FeedbackDescription = "Some bug text to copy and clear";
            vm.CopyFeedbackCommand.Execute(null);

            vm.DiscardFeedbackCommand.Execute(null);

            Assert.False(vm.ShowFeedbackDialog);
        }
    }
}
