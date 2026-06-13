using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Media;
using System.Net;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Localization;
using QuickMediaIngest.Core.Services;
using QuickMediaIngest.Data;
using QuickMediaIngest;


namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [RelayCommand] private void RefreshUpdate() => CheckUpdates(force: true);
        [RelayCommand]
        private async Task RefreshAllSources()
        {
            await ScanDrivesAsync();
            _sourceItemsCache.Clear();
            _thumbnailByItemKey.Clear();
            ClearThumbnailDiskCache();

            if (SelectedSource is UnifiedSourceItem || SelectedSource == null)
            {
                await LoadUnifiedSourceItemsAsync(forceRefresh: true);
                if (SelectedSource == null)
                {
                    SelectedSource = _unifiedSource;
                }
                return;
            }

            LoadSourceItems(SelectedSource, forceRefresh: true);
        }
        // BrowseDestination command removed; UI entry deleted.
        [RelayCommand] private void Rescan() => OpenDriveSelectionDialog();
        [RelayCommand] private void BrowseScanPath() => ExecuteBrowseScanPath();
        [RelayCommand] private void BuildSelectedPreviews() => ExecuteBuildSelectedPreviews();
        [RelayCommand] private void SelectAllShoots() => SetAllShootsSelected(true);
        [RelayCommand] private void DeselectAllShoots() => SetAllShootsSelected(false);
        [RelayCommand]
        private async Task ConfirmDriveSelection() => await ExecuteConfirmDriveSelectionAsync();
        [RelayCommand] private void CancelDriveSelection() => ShowDriveSelectionDialog = false;
        [RelayCommand] private void SkipFolder(string? folderPath) => ExecuteSkipFolder(folderPath);

        [RelayCommand]
        private void ExitApplication()
        {
            SaveConfig();
            Application.Current.Shutdown(0);
        }

        [RelayCommand]
        private void CancelActiveImport()
        {
            if (_importCancellationSource == null)
            {
                return;
            }

            if (ConfirmCancelImportRequest)
            {
                MessageBoxResult r = MessageBox.Show(
                    AppLocalizer.Get("Msg_CancelImport_ConfirmBody"),
                    AppLocalizer.Get("Msg_CancelImport_ConfirmTitle"),
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Warning);
                if (r != MessageBoxResult.OK)
                {
                    return;
                }
            }

            try
            {
                _importCancellationSource.Cancel();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Cancel import signaled.");
            }
        }

        [RelayCommand]
        private void ClearNotificationFeed() => NotificationFeed.Clear();

        [RelayCommand]
        private void ShowShortcutsHelp()
        {
            var owner = Application.Current.MainWindow;
            var win = new QuickMediaIngest.ShortcutsHelpWindow { Owner = owner };
            win.ShowDialog();
        }

        [RelayCommand]
        private void OpenImportReportsFolder()
        {
            try
            {
                string dir = Path.Combine(DestinationRoot, "_ImportReports");
                Directory.CreateDirectory(dir);
                Process.Start(new ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not open _ImportReports folder.");
            }
        }
        [RelayCommand]
        private async Task RemoveDriveExclusion(string? deviceId) => await ExecuteRemoveDriveExclusionAsync(deviceId);
        [RelayCommand] private void RemoveSkippedFolderRule(SkippedFolderRuleEntry? entry) => ExecuteRemoveSkippedFolderRule(entry);
        public void SelectAllVisible() => SetAllShootsSelected(true);
        public void DeselectAllVisible() => SetAllShootsSelected(false);

        // Keyboard accelerator commands for UI
        public ICommand SelectAllCommand => new RelayCommand(SelectAllShoots);
        public ICommand CancelCommand => new RelayCommand(DismissTopOverlay);

        partial void OnSelectedFtpPresetFolderChanged(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                FtpRemoteFolder = value;
        }
        partial void OnTimeBetweenShootsHoursChanged(int value)
        {
            int clamped = Math.Clamp(value, 1, 24);
            if (timeBetweenShootsHours != clamped)
                TimeBetweenShootsHours = clamped;
            SaveConfig();
            RebuildGroupsFromCurrentItems();
        }

        private void CheckUpdates(bool force = false)
        {
            System.Threading.Tasks.Task.Run(async () =>
            {
                _logger.LogInformation("Checking for updates from view model. Force={Force}", force);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsCheckingForUpdate = true;
                    UpdateStatus = AppLocalizer.Get("About_Update_Checking");
                    UpdateProgress = 0.0;
                });

                var checkResult = await _updateService.CheckForUpdateAsync(UpdateIntervalHours, force, UpdatePackageType);
                string? url = checkResult.DownloadUrl;

                if (!string.IsNullOrEmpty(url))
                {
                    string assetLabel = GetUpdateAssetLabel(url);
                    string notifyKey = !string.IsNullOrWhiteSpace(checkResult.RemoteVersionTag)
                        ? checkResult.RemoteVersionTag!
                        : url;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        UpdateUrl = url;
                        ShowUpdateBanner = true;
                        IsUpdateAvailable = true;
                        UpdateStatus = AppLocalizer.Format("Vm_Update_Available", assetLabel);
                        StatusMessage = AppLocalizer.Format("Vm_Update_FoundGithub", assetLabel);
                        UpdateProgress = 0.0;

                        bool shouldPopup = !string.Equals(notifyKey, LastNotifiedUpdateTag, StringComparison.OrdinalIgnoreCase);
                        if (shouldPopup)
                        {
                            MessageBox.Show(
                                AppLocalizer.Format("Vm_Update_PopupBody", checkResult.RemoteVersionTag ?? "", assetLabel),
                                AppLocalizer.Get("Vm_Update_PopupTitle"),
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                            LastNotifiedUpdateTag = notifyKey;
                            SaveConfig();
                        }
                    });
                }
                else if (force)
                {
                    string expected = UpdatePackageType == "Installer" ? "QuickMediaIngest.msi" : "QuickMediaIngest.exe";
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusMessage = AppLocalizer.Format("Vm_Update_NoUpdates", expected);
                        UpdateStatus = AppLocalizer.Format("Vm_Update_StatusNoUpdates", expected);
                        IsUpdateAvailable = false;
                    });
                }

                Application.Current.Dispatcher.Invoke(() => IsCheckingForUpdate = false);
            });
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

        private async void ExecuteDownloadUpdate()
        {
            if (string.IsNullOrEmpty(UpdateUrl)) return;

            _logger.LogInformation("Starting update download from {UpdateUrl}.", UpdateUrl);

            IsDownloadingUpdate = true;
            UpdateProgress = 0.0;
            UpdateStatus = AppLocalizer.Get("Vm_Update_StartingDownload");
            ShowUpdateBanner = false;

            try
            {
                string ext = Path.GetExtension(UpdateUrl).ToLowerInvariant();
                string fileName = ext switch
                {
                    ".msi" => "QuickMediaIngest_Update.msi",
                    ".exe" => "QuickMediaIngest_Update.exe",
                    _ => string.Empty
                };

                if (!string.IsNullOrEmpty(fileName))
                {
                    string updateTempDir = Path.Combine(Path.GetTempPath(), "QuickMediaIngest", "updates");
                    Directory.CreateDirectory(updateTempDir);
                    string versionSuffix = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                    string tempPath = Path.Combine(
                        updateTempDir,
                        $"{Path.GetFileNameWithoutExtension(fileName)}_{versionSuffix}{Path.GetExtension(fileName)}");

                    using var client = new System.Net.Http.HttpClient();
                    using var response = await client.GetAsync(UpdateUrl, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    var contentLength = response.Content.Headers.ContentLength;
                    using var contentStream = await response.Content.ReadAsStreamAsync();
                    using var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    var buffer = new byte[81920];
                    long totalRead = 0;
                    int read;
                    var sw = Stopwatch.StartNew();
                    while ((read = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
                    {
                        await fs.WriteAsync(buffer.AsMemory(0, read));
                        totalRead += read;

                        // Compute progress, speed and ETA
                        double percent = 0.0;
                        if (contentLength.HasValue && contentLength.Value > 0)
                        {
                            percent = Math.Round((double)totalRead / contentLength.Value * 100.0, 1);
                        }

                        double bytesPerSecond = sw.Elapsed.TotalSeconds > 0 ? totalRead / sw.Elapsed.TotalSeconds : 0;
                        string speedText = bytesPerSecond > 0 ? $"{(bytesPerSecond / (1024d * 1024d)):0.00} MB/s" : "-- MB/s";
                        string etaText = "--:--:--";
                        if (contentLength.HasValue && bytesPerSecond > 0)
                        {
                            double remaining = Math.Max(0, contentLength.Value - totalRead);
                            etaText = TimeSpan.FromSeconds(remaining / bytesPerSecond).ToString(@"hh\:mm\:ss");
                        }

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            UpdateProgress = percent;
                            UpdateStatus = contentLength.HasValue
                                ? AppLocalizer.Format("Vm_Update_DownloadingPercent", percent)
                                : AppLocalizer.Format("Vm_Update_DownloadingBytes", totalRead / 1024);
                            UpdateDownloadSpeedText = speedText;
                            UpdateDownloadEtaText = etaText;
                        });
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        UpdateProgress = 100.0;
                        UpdateStatus = AppLocalizer.Get("Vm_Update_DownloadComplete");
                    });

                    if (ext == ".msi" || ext == ".exe")
                    {
                        string currentExePath = Environment.ProcessPath
                            ?? Process.GetCurrentProcess().MainModule?.FileName
                            ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(currentExePath) || !File.Exists(currentExePath))
                        {
                            throw new InvalidOperationException("Unable to locate current executable path for update handoff.");
                        }

                        string updaterScript = BuildUpdateHandoffScript(
                            tempPath,
                            ext,
                            currentExePath,
                            Process.GetCurrentProcess().Id,
                            UpdatePackageType);

                        Process.Start(new ProcessStartInfo("cmd.exe", $"/c \"{updaterScript}\"")
                        {
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        });

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            UpdateStatus = AppLocalizer.Get("Vm_Update_HandoffClosing");
                        });
                        Application.Current.Shutdown();
                    }
                }
                else
                {
                    // Non-executable update: open in browser
                    OpenUrl(UpdateUrl);
                    Application.Current.Dispatcher.Invoke(() => UpdateStatus = AppLocalizer.Get("Vm_Update_OpenedBrowser"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update download failed.");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateStatus = AppLocalizer.Format("Vm_Update_DownloadFailed", ex.Message);
                    IsUpdateAvailable = true;
                    ShowUpdateBanner = true;
                });
            }
            finally
            {
                IsDownloadingUpdate = false;
            }
        }
    }
}
