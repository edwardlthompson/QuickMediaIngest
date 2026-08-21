#nullable enable
using System;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using Application = System.Windows.Application;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core;
using QuickMediaIngest.Localization;

namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel
    {
        private UpdateCheckResult _pendingUpdateResult;
        private bool _hasPendingUpdatePrompt;

        [RelayCommand]
        private void OpenDonateVenmo() => OpenUrl(DonationLinks.Venmo);

        [RelayCommand]
        private void AcceptDonateNudge()
        {
            MarkDonateNudgeSeen();
            ShowDonateNudgeDialog = false;
            OpenUrl(DonationLinks.Venmo);
            ShowPendingUpdatePromptIfReady();
        }

        [RelayCommand]
        private void DeclineDonateNudge()
        {
            MarkDonateNudgeSeen();
            ShowDonateNudgeDialog = false;
            ShowPendingUpdatePromptIfReady();
        }

        [RelayCommand]
        private void InstallPendingUpdate()
        {
            ShowUpdatePromptDialog = false;
            if (!string.IsNullOrWhiteSpace(UpdateUrl))
            {
                OpenUrl(UpdateUrl);
            }
        }

        [RelayCommand]
        private void LaterPendingUpdate()
        {
            if (!string.IsNullOrWhiteSpace(_pendingUpdateResult.RemoteVersionTag))
            {
                try
                {
                    UpdateDonatePreferences prefs = _updateDonateStore.Load();
                    UpdateDonateState.DismissProductVersion(prefs, _pendingUpdateResult.RemoteVersionTag);
                    _updateDonateStore.Save(prefs);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not persist dismissed update version.");
                }
            }

            ShowUpdatePromptDialog = false;
        }

        private void EvaluateDonateNudge()
        {
            try
            {
                UpdateDonatePreferences prefs = _updateDonateStore.Load();
                string current = AppVersion;
                if (UpdateDonateState.TryRecordFirstInstalledVersion(prefs, current))
                {
                    _updateDonateStore.Save(prefs);
                    return;
                }

                if (UpdateDonateState.ShouldShowDonateNudge(
                    current,
                    prefs.RecordedInstalledVersion,
                    prefs.DonateNudgeSeenForVersion))
                {
                    ShowDonateNudgeDialog = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Donate nudge evaluation failed.");
            }
        }

        private void MarkDonateNudgeSeen()
        {
            try
            {
                UpdateDonatePreferences prefs = _updateDonateStore.Load();
                UpdateDonateState.MarkDonateNudgeSeen(prefs, AppVersion);
                _updateDonateStore.Save(prefs);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not persist donate nudge seen version.");
            }
        }

        private void CheckUpdates(bool force = false)
        {
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                _logger.LogInformation("Checking for updates from view model. Force={Force}", force);
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    IsCheckingForUpdate = true;
                    UpdateStatus = AppLocalizer.Get("About_Update_Checking");
                    UpdateProgress = 0.0;
                });

                UpdateCheckResult checkResult = await _updateService.CheckForUpdateAsync(UpdateIntervalHours, force, UpdatePackageType);
                string? url = checkResult.DownloadUrl;

                if (!string.IsNullOrEmpty(url))
                {
                    string assetLabel = GetUpdateAssetLabel(url);
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        ApplyUpdateCheckResult(checkResult, url, assetLabel, showPrompt: true);
                    });
                }
                else if (force)
                {
                    string expected = UpdatePackageType == "Installer"
                        ? "QuickMediaIngest-x64-setup.msi"
                        : "QuickMediaIngest-x64.exe";
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        StatusMessage = AppLocalizer.Format("Vm_Update_NoUpdates", expected);
                        UpdateStatus = AppLocalizer.Format("Vm_Update_StatusNoUpdates", expected);
                        IsUpdateAvailable = false;
                    });
                }

                Application.Current?.Dispatcher.Invoke(() => IsCheckingForUpdate = false);
            });
        }

        private void ApplyUpdateCheckResult(UpdateCheckResult checkResult, string url, string assetLabel, bool showPrompt)
        {
            UpdateUrl = url;
            ShowUpdateBanner = true;
            IsUpdateAvailable = true;
            UpdateStatus = AppLocalizer.Format("Vm_Update_Available", assetLabel);
            StatusMessage = AppLocalizer.Format("Vm_Update_FoundGithub", assetLabel);
            UpdateProgress = 0.0;
            _pendingUpdateResult = checkResult;
            _hasPendingUpdatePrompt = showPrompt;

            if (showPrompt && !ShowDonateNudgeDialog)
            {
                ShowUpdatePromptDialog = true;
                _hasPendingUpdatePrompt = false;
            }
        }

        private void ShowPendingUpdatePromptIfReady()
        {
            if (!_hasPendingUpdatePrompt || string.IsNullOrWhiteSpace(_pendingUpdateResult.DownloadUrl))
            {
                return;
            }

            ShowUpdatePromptDialog = true;
            _hasPendingUpdatePrompt = false;
        }

        private static string GetUpdateAssetLabel(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return "release page";
            }

            try
            {
                var uri = new Uri(url);
                string fileName = Path.GetFileName(uri.LocalPath);
                return string.IsNullOrWhiteSpace(fileName) ? "release page" : Uri.UnescapeDataString(fileName);
            }
            catch
            {
                return "release page";
            }
        }
    }
}
