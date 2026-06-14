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
using QuickMediaIngest.Services;
using QuickMediaIngest;


namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {

        private void OpenUrl(string url) => _shellService.OpenUrl(url);

        private string ResolveFtpPassword() =>
            FtpSourceCredentials.ResolvePassword(FtpPass, FtpHost, FtpPort, FtpHost, _ftpCredentialStore);

        private string ResolveFtpPasswordForSource(FtpSourceItem ftp) =>
            FtpSourceCredentials.ResolvePassword(ftp.Pass, ftp.Host, ftp.Port, ftp.Host, _ftpCredentialStore);

        private void EnsureFtpSourceCredentials(FtpSourceItem ftp)
        {
            ftp.Pass = ResolveFtpPasswordForSource(ftp);
        }

        private FtpEndpoint ToFtpEndpoint(FtpSourceItem ftp)
        {
            EnsureFtpSourceCredentials(ftp);
            string pass = ResolveFtpPasswordForSource(ftp);
            ftp.Pass = pass;
            return new FtpEndpoint(ftp.Host, ftp.Port, ftp.User, pass);
        }

        private void ExecuteSaveFtp()
        {
            if (string.IsNullOrEmpty(FtpHost)) return;

            string remoteFolder = NormalizeFtpPath(string.IsNullOrWhiteSpace(FtpRemoteFolder) ? "/DCIM" : FtpRemoteFolder);
            string password = ResolveFtpPassword();

            var ftp = new FtpSourceItem
            {
                Host = FtpHostNormalizer.Normalize(FtpHost),
                Port = FtpPort,
                User = FtpUser,
                Pass = password,
                RemoteFolder = remoteFolder
            };

            bool exists = Sources.OfType<FtpSourceItem>().Any(f => f.Host == ftp.Host && f.RemoteFolder == ftp.RemoteFolder);
            if (!exists) Sources.Add(ftp);

            FtpRemoteFolder = remoteFolder;
            ShowAddFtpDialog = false;
            IsFirstRun = false;
            SaveConfig();
            SelectedSource = ftp; // triggers scan instantly
        }

        private async void ExecuteTestFtpConnection()
        {
            if (IsTestingFtp)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(FtpHost))
            {
                StatusMessage = AppLocalizer.Get("Vm_Ftp_EnterHostBeforeTest");
                FtpDialogStatusMessage = StatusMessage;
                return;
            }

            if (FtpPort <= 0 || FtpPort > 65535)
            {
                StatusMessage = AppLocalizer.Get("Vm_Ftp_PortRange");
                FtpDialogStatusMessage = StatusMessage;
                return;
            }

            string remotePath = NormalizeFtpPath(FtpRemoteFolder);
            IsTestingFtp = true;
            StatusMessage = AppLocalizer.Format("Vm_Ftp_TestingConnection", FtpHost, FtpPort, remotePath);
            FtpDialogStatusMessage = StatusMessage;
            _logger.LogInformation("Testing FTP connection to {Host}:{Port}{RemotePath}.", FtpHost, FtpPort, remotePath);

            try
            {
                string password = ResolveFtpPassword();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var result = await Task.Run(async () =>
                    await _ftpWorkflowService.TestConnectionAsync(
                        FtpHost,
                        FtpPort,
                        FtpUser,
                        password,
                        remotePath,
                        15,
                        timeout.Token));

                if (result.Success)
                {
                    HasLastFtpReconnectFailure = false;
                    RefreshUxEmptyStateHints();
                    IsFirstRun = false;
                    SaveConfig();
                }

                StatusMessage = result.Success
                    ? AppLocalizer.Format("Vm_Ftp_TestSuccess", result.Message)
                    : AppLocalizer.Format("Vm_Ftp_TestFailed", result.Message);
                FtpDialogStatusMessage = StatusMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FTP connection test failed for {Host}:{Port}{RemotePath}.", FtpHost, FtpPort, remotePath);
                StatusMessage = AppLocalizer.Format("Vm_Ftp_TestFailed", ex.Message);
                FtpDialogStatusMessage = StatusMessage;
            }
            finally
            {
                IsTestingFtp = false;
            }
        }

        private async void ExecuteBrowseFtpFolders()
        {
            if (IsBrowsingFtpFolders || IsTestingFtp)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(FtpHost))
            {
                StatusMessage = AppLocalizer.Get("Vm_Ftp_EnterHostBeforeBrowse");
                FtpDialogStatusMessage = StatusMessage;
                return;
            }

            if (FtpPort <= 0 || FtpPort > 65535)
            {
                StatusMessage = AppLocalizer.Get("Vm_Ftp_PortRange");
                FtpDialogStatusMessage = StatusMessage;
                return;
            }

            string remotePath = NormalizeFtpPath(string.IsNullOrWhiteSpace(FtpRemoteFolder) ? "/DCIM" : FtpRemoteFolder);
            IsBrowsingFtpFolders = true;
            StatusMessage = AppLocalizer.Format("Vm_Ftp_BrowsingFolders", FtpHost, FtpPort, remotePath);
            FtpDialogStatusMessage = StatusMessage;
            _logger.LogInformation("Browsing FTP folders at {Host}:{Port}{RemotePath}.", FtpHost, FtpPort, remotePath);

            try
            {
                string password = ResolveFtpPassword();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var folders = await Task.Run(async () =>
                    await _ftpScanner.ListDirectoriesAsync(
                        FtpHost,
                        FtpPort,
                        FtpUser,
                        password,
                        remotePath,
                        15,
                        timeout.Token));

                BrowsedFtpFolders.Clear();
                BrowsedFtpFolders.Add(new FtpFolderOption { Path = remotePath, Label = AppLocalizer.Format("Vm_Ftp_BrowseCurrentFolder", remotePath) });

                string? parentPath = GetParentFtpPath(remotePath);
                if (!string.IsNullOrEmpty(parentPath))
                {
                    BrowsedFtpFolders.Add(new FtpFolderOption { Path = parentPath, Label = AppLocalizer.Format("Vm_Ftp_BrowseParentFolder", parentPath) });
                }

                foreach (string folder in folders)
                {
                    if (!BrowsedFtpFolders.Any(option => string.Equals(option.Path, folder, StringComparison.OrdinalIgnoreCase)))
                    {
                        BrowsedFtpFolders.Add(new FtpFolderOption { Path = folder, Label = folder });
                    }
                }

                SelectedBrowsedFtpFolder = BrowsedFtpFolders.FirstOrDefault();
                StatusMessage = BrowsedFtpFolders.Count > 0
                    ? AppLocalizer.Format("Vm_Ftp_BrowseFoundFolders", BrowsedFtpFolders.Count, remotePath)
                    : AppLocalizer.Format("Vm_Ftp_BrowseNoFolders", remotePath);
                FtpDialogStatusMessage = StatusMessage;
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("FTP browse timed out for {Host}:{Port}{RemotePath}.", FtpHost, FtpPort, remotePath);
                StatusMessage = AppLocalizer.Format("Vm_Ftp_BrowseTimedOut", remotePath);
                FtpDialogStatusMessage = StatusMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FTP folder browse failed for {Host}:{Port}{RemotePath}.", FtpHost, FtpPort, remotePath);
                StatusMessage = AppLocalizer.Format("Vm_Ftp_BrowseFailed", ex.Message);
                FtpDialogStatusMessage = StatusMessage;
            }
            finally
            {
                IsBrowsingFtpFolders = false;
            }
        }

        private void ExecuteUseBrowsedFtpFolder()
        {
            if (SelectedBrowsedFtpFolder == null)
            {
                StatusMessage = AppLocalizer.Get("Vm_Ftp_ChooseBrowsedFolder");
                return;
            }

            FtpRemoteFolder = SelectedBrowsedFtpFolder.Path;
            SelectedFtpPresetFolder = SelectedBrowsedFtpFolder.Path;
            StatusMessage = AppLocalizer.Format("Vm_Ftp_FolderSelected", SelectedBrowsedFtpFolder.Path);
            FtpDialogStatusMessage = StatusMessage;
        }

        private async Task TryReconnectLastFtpAsync()
        {
            if (!AutoReconnectLastFtp || string.IsNullOrWhiteSpace(FtpHost))
            {
                return;
            }

            string remotePath = NormalizeFtpPath(string.IsNullOrWhiteSpace(FtpRemoteFolder) ? "/DCIM" : FtpRemoteFolder);
            string password = ResolveFtpPassword();
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                var result = await _ftpWorkflowService.TestConnectionAsync(
                    FtpHost,
                    FtpPort,
                    FtpUser,
                    password,
                    remotePath,
                    8,
                    timeout.Token);

                if (!result.Success)
                {
                    HasLastFtpReconnectFailure = true;
                    RefreshUxEmptyStateHints();
                    StatusMessage = AppLocalizer.Format("Vm_Ftp_LastSourceUnreachable", FtpHost, FtpPort, remotePath);
                    return;
                }

                HasLastFtpReconnectFailure = false;
                RefreshUxEmptyStateHints();

                var ftp = new FtpSourceItem
                {
                    Host = FtpHostNormalizer.Normalize(FtpHost),
                    Port = FtpPort,
                    User = FtpUser,
                    Pass = password,
                    RemoteFolder = remotePath
                };

                bool exists = Sources.OfType<FtpSourceItem>().Any(s =>
                    string.Equals(s.Host, ftp.Host, StringComparison.OrdinalIgnoreCase) &&
                    s.Port == ftp.Port &&
                    string.Equals(NormalizeFtpPath(s.RemoteFolder), remotePath, StringComparison.OrdinalIgnoreCase));

                if (!exists)
                {
                    Sources.Add(ftp);
                }
                else
                {
                    var existing = Sources.OfType<FtpSourceItem>().First(s =>
                        string.Equals(s.Host, ftp.Host, StringComparison.OrdinalIgnoreCase) &&
                        s.Port == ftp.Port &&
                        string.Equals(NormalizeFtpPath(s.RemoteFolder), remotePath, StringComparison.OrdinalIgnoreCase));
                    existing.Pass = password;
                }

                StatusMessage = AppLocalizer.Format("Vm_Ftp_Reconnected", FtpHost, FtpPort, remotePath);
                SaveConfig();
            }
            catch
            {
                HasLastFtpReconnectFailure = true;
                RefreshUxEmptyStateHints();
                StatusMessage = AppLocalizer.Format("Vm_Ftp_LastSourceUnreachable", FtpHost, FtpPort, remotePath);
            }
        }
    }
}
