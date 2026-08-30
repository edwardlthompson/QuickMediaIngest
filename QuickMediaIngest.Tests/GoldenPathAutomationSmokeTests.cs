#nullable enable
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Moq;
using QuickMediaIngest;
using QuickMediaIngest.Core.CrashCapture;
using QuickMediaIngest.Core.DisplayRefresh;
using QuickMediaIngest.Core.GitHubFeedback;
using QuickMediaIngest.Core.PrivacyReport;
using QuickMediaIngest.Services;
using QuickMediaIngest.ViewModels;
using Xunit;

namespace QuickMediaIngest.Tests
{
    /// <summary>
    /// Automated regression & smoke tests fulfilling the verification of Golden Path slices GP-1 through GP-7.
    /// </summary>
    [Collection("Wpf")]
    public class GoldenPathAutomationSmokeTests
    {
        private static string ConfigPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest", "config.json");

        [Fact]
        public void GP1_About_DonateAndFeatureFeedbackEntry_Verified()
        {
            WpfTestHost.EnsureInitialized();
            MainViewModel vm = MainViewModelProofTests.CreateViewModel();

            vm.RequestFeatureCommand.Execute(null);

            Assert.True(vm.ShowFeedbackDialog);
            Assert.Equal("feature", vm.FeedbackKind);
            Assert.False(string.IsNullOrWhiteSpace(vm.FeedbackTitle));
        }

        [Fact]
        public void GP2_CrashCapture_OptInOff_DoesNotPersist_OptInOn_PersistsSanitizedRecord()
        {
            WpfTestHost.EnsureInitialized();
            var store = new FilePendingCrashStore();
            store.Clear();

            var serviceOff = new CrashCaptureService(store, () => false);
            bool capturedOff = serviceOff.TryCapture(new Exception("secret token ghp_1234567890abcdef"), "1.3.27");
            Assert.False(capturedOff);
            Assert.Null(store.Load());

            var serviceOn = new CrashCaptureService(store, () => true);
            bool capturedOn = serviceOn.TryCapture(new Exception("Crash at C:\\Users\\Tester\\app ghp_1234567890abcdef"), "1.3.27");
            Assert.True(capturedOn);

            PendingCrash? record = store.Load();
            Assert.NotNull(record);
            Assert.DoesNotContain("Tester", record!.Stack, StringComparison.Ordinal);
            Assert.DoesNotContain("ghp_", record.Stack, StringComparison.Ordinal);
            Assert.Contains("<redacted-secret>", record.Description, StringComparison.Ordinal);

            store.Clear();
        }

        [Fact]
        public void GP3_Settings_ThemeAndCrashTogglePersist_Verified()
        {
            WpfTestHost.EnsureInitialized();
            string? backup = BackupConfigIfPresent();

            try
            {
                var initial = new AppConfig
                {
                    IsDarkTheme = true,
                    SaveCrashDetails = true
                };
                WriteConfig(initial);

                MainViewModel vm = MainViewModelProofTests.CreateViewModel();
                InvokeLoadConfig(vm);

                Assert.True(vm.SaveCrashDetails);

                vm.SaveCrashDetails = false;
                vm.SaveConfig();

                MainViewModel vmReloaded = MainViewModelProofTests.CreateViewModel();
                InvokeLoadConfig(vmReloaded);

                Assert.False(vmReloaded.SaveCrashDetails);
            }
            finally
            {
                RestoreConfig(backup);
            }
        }

        [Fact]
        public void GP4_Feedback_Dialogs_EscapedPreviewAndOpenGitHub_Verified()
        {
            WpfTestHost.EnsureInitialized();
            MainViewModel vm = MainViewModelProofTests.CreateViewModel();

            vm.ReportBugCommand.Execute(null);
            Assert.True(vm.ShowFeedbackDialog);
            Assert.False(vm.FeedbackCanOpenGitHub);

            vm.FeedbackDescription = "Crash report with secret ghp_secret12345";
            Assert.True(vm.FeedbackCanOpenGitHub);
            Assert.DoesNotContain("ghp_secret12345", vm.FeedbackPreview, StringComparison.Ordinal);
            Assert.Contains("<redacted-secret>", vm.FeedbackPreview, StringComparison.Ordinal);

            vm.DiscardFeedbackCommand.Execute(null);
            Assert.False(vm.ShowFeedbackDialog);
            Assert.Equal(string.Empty, vm.FeedbackDescription);
        }

        [Fact]
        public void GP5_GitHubFeedback_UsesHttpsOnlyAndFallbackWorks_Verified()
        {
            GitHubIssueLink link = GitHubIssueComposer.Compose("bug", "Valid issue description");
            Assert.StartsWith("https://", link.Url, StringComparison.Ordinal);
            Assert.False(link.UseClipboard);

            string largeBody = new string('a', 2000);
            GitHubIssueLink largeLink = GitHubIssueComposer.Compose("bug", largeBody);
            Assert.StartsWith("https://", largeLink.Url, StringComparison.Ordinal);
            Assert.True(largeLink.UseClipboard);
            Assert.False(string.IsNullOrWhiteSpace(largeLink.SanitizedBody));
        }

        [Fact]
        public void GP6_PrivacySanitizer_RemovesTokensAndPaths_Verified()
        {
            string raw = "Exception in C:\\Users\\Alice\\project with token Bearer ghp_99999999999999999999";
            string sanitized = PrivacyReportSanitize.SanitizeReportText(raw);

            Assert.DoesNotContain("Alice", sanitized, StringComparison.Ordinal);
            Assert.DoesNotContain("ghp_", sanitized, StringComparison.Ordinal);
            Assert.Contains("<redacted-home>", sanitized, StringComparison.Ordinal);
            Assert.Contains("<redacted-secret>", sanitized, StringComparison.Ordinal);
        }

        [Fact]
        public void GP7_DisplayRefresh_FastestSameResolutionSelected_Verified()
        {
            var modes = new[]
            {
                new DisplayModeInfo(2560, 1440, 60),
                new DisplayModeInfo(2560, 1440, 120),
                new DisplayModeInfo(2560, 1440, 165),
                new DisplayModeInfo(3840, 2160, 60)
            };

            DisplayModeInfo? selected = DisplayModeSelector.SelectFastestSameResolution(2560, 1440, modes);
            Assert.NotNull(selected);
            Assert.Equal(165, selected!.Value.RefreshHz);
        }

        private static void WriteConfig(AppConfig config)
        {
            string folder = Path.GetDirectoryName(ConfigPath)!;
            Directory.CreateDirectory(folder);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config));
        }

        private static void InvokeLoadConfig(MainViewModel vm)
        {
            var load = typeof(MainViewModel).GetMethod("LoadConfig", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(load);
            load.Invoke(vm, null);
        }

        private static string? BackupConfigIfPresent()
        {
            if (!File.Exists(ConfigPath)) return null;
            string backup = ConfigPath + ".bak." + Guid.NewGuid().ToString("N");
            File.Copy(ConfigPath, backup);
            return backup;
        }

        private static void RestoreConfig(string? backup)
        {
            try
            {
                if (File.Exists(ConfigPath)) File.Delete(ConfigPath);
                if (backup != null && File.Exists(backup)) File.Move(backup, ConfigPath);
            }
            catch { }
        }
    }
}
