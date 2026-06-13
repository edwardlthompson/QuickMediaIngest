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

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not open URL: {Url}", url);
            }
        }

        private static string BuildUpdateHandoffScript(string downloadedUpdatePath, string ext, string currentExePath, int currentPid, string packageType)
        {
            string tempScript = Path.Combine(Path.GetTempPath(), "QuickMediaIngest", "updates", $"apply-update-{DateTime.UtcNow:yyyyMMddHHmmssfff}.cmd");
            Directory.CreateDirectory(Path.GetDirectoryName(tempScript) ?? Path.GetTempPath());

            string script = $@"@echo off
setlocal enableextensions
set ""QMI_UPDATE_FILE={downloadedUpdatePath}""
set ""QMI_CURRENT_EXE={currentExePath}""
set ""QMI_PID={currentPid}""
set ""QMI_PACKAGE={packageType}""
set ""QMI_EXT={ext}""

for /L %%i in (1,1,180) do (
  tasklist /FI ""PID eq %QMI_PID%"" | findstr /I /C:""%QMI_PID%"" >nul
  if errorlevel 1 goto :ready
  timeout /t 1 /nobreak >nul
)

:ready
if /I ""%QMI_EXT%""=="".msi"" (
  start """" /wait msiexec /i ""%QMI_UPDATE_FILE%"" /passive /norestart
  start """" ""%QMI_CURRENT_EXE%""
  goto :cleanup
)

if /I ""%QMI_EXT%""=="".exe"" (
  if /I ""%QMI_PACKAGE%""==""Portable"" (
    copy /Y ""%QMI_UPDATE_FILE%"" ""%QMI_CURRENT_EXE%"" >nul
    start """" ""%QMI_CURRENT_EXE%""
  ) else (
    start """" ""%QMI_UPDATE_FILE%""
  )
  goto :cleanup
)

:cleanup
del /Q ""%QMI_UPDATE_FILE%"" >nul 2>nul
del /Q ""%~f0"" >nul 2>nul
";

            File.WriteAllText(tempScript, script, Encoding.ASCII);
            return tempScript;
        }

        private void ExecuteSaveFtp()
        {
            if (string.IsNullOrEmpty(FtpHost)) return;

            string remoteFolder = NormalizeFtpPath(string.IsNullOrWhiteSpace(FtpRemoteFolder) ? "/DCIM" : FtpRemoteFolder);

            var ftp = new FtpSourceItem
            {
                Host = FtpHost,
                Port = FtpPort,
                User = FtpUser,
                Pass = FtpPass,
                RemoteFolder = remoteFolder
            };

            bool exists = Sources.OfType<FtpSourceItem>().Any(f => f.Host == ftp.Host && f.RemoteFolder == ftp.RemoteFolder);
            if (!exists) Sources.Add(ftp);

            FtpRemoteFolder = remoteFolder;
            ShowAddFtpDialog = false;
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
            StatusMessage = $"Testing FTP connection to {FtpHost}:{FtpPort}{remotePath}...";
            FtpDialogStatusMessage = StatusMessage;
            _logger.LogInformation("Testing FTP connection to {Host}:{Port}{RemotePath}.", FtpHost, FtpPort, remotePath);

            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var result = await Task.Run(async () =>
                    await _ftpWorkflowService.TestConnectionAsync(
                        FtpHost,
                        FtpPort,
                        FtpUser,
                        FtpPass,
                        remotePath,
                        15,
                        timeout.Token));

                if (result.Success)
                {
                    HasLastFtpReconnectFailure = false;
                    RefreshUxEmptyStateHints();
                }

                StatusMessage = result.Success
                    ? $"FTP test successful. {result.Message} Use /DCIM or /DCIM/Camera for faster phone scans."
                    : $"FTP test failed. {result.Message}";
                FtpDialogStatusMessage = StatusMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FTP connection test failed for {Host}:{Port}{RemotePath}.", FtpHost, FtpPort, remotePath);
                StatusMessage = $"FTP test failed. {ex.Message}";
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
            StatusMessage = $"Browsing FTP folders at {FtpHost}:{FtpPort}{remotePath}...";
            FtpDialogStatusMessage = StatusMessage;
            _logger.LogInformation("Browsing FTP folders at {Host}:{Port}{RemotePath}.", FtpHost, FtpPort, remotePath);

            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var folders = await Task.Run(async () =>
                    await _ftpScanner.ListDirectoriesAsync(
                        FtpHost,
                        FtpPort,
                        FtpUser,
                        FtpPass,
                        remotePath,
                        15,
                        timeout.Token));

                BrowsedFtpFolders.Clear();
                BrowsedFtpFolders.Add(new FtpFolderOption { Path = remotePath, Label = $"Use current folder ({remotePath})" });

                string? parentPath = GetParentFtpPath(remotePath);
                if (!string.IsNullOrEmpty(parentPath))
                {
                    BrowsedFtpFolders.Add(new FtpFolderOption { Path = parentPath, Label = $"Parent folder ({parentPath})" });
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
                    ? $"Connected to FTP. Found {BrowsedFtpFolders.Count} folder option(s) under {remotePath}."
                    : $"Connected to FTP, but no folders were found under {remotePath}.";
                FtpDialogStatusMessage = StatusMessage;
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("FTP browse timed out for {Host}:{Port}{RemotePath}.", FtpHost, FtpPort, remotePath);
                StatusMessage = $"Connected to FTP, but browsing {remotePath} timed out. Try /DCIM or /DCIM/Camera.";
                FtpDialogStatusMessage = StatusMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FTP folder browse failed for {Host}:{Port}{RemotePath}.", FtpHost, FtpPort, remotePath);
                StatusMessage = $"FTP folder browse failed. {ex.Message}";
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
            StatusMessage = $"FTP folder selected: {SelectedBrowsedFtpFolder.Path}";
            FtpDialogStatusMessage = StatusMessage;
        }

        private async Task TryReconnectLastFtpAsync()
        {
            if (!AutoReconnectLastFtp || string.IsNullOrWhiteSpace(FtpHost))
            {
                return;
            }

            string remotePath = NormalizeFtpPath(string.IsNullOrWhiteSpace(FtpRemoteFolder) ? "/DCIM" : FtpRemoteFolder);
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                var result = await _ftpWorkflowService.TestConnectionAsync(
                    FtpHost,
                    FtpPort,
                    FtpUser,
                    FtpPass,
                    remotePath,
                    8,
                    timeout.Token);

                if (!result.Success)
                {
                    HasLastFtpReconnectFailure = true;
                    RefreshUxEmptyStateHints();
                    StatusMessage = $"Last FTP source not reachable: {FtpHost}:{FtpPort}{remotePath}";
                    return;
                }

                HasLastFtpReconnectFailure = false;
                RefreshUxEmptyStateHints();

                var ftp = new FtpSourceItem
                {
                    Host = FtpHost,
                    Port = FtpPort,
                    User = FtpUser,
                    Pass = FtpPass,
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

                StatusMessage = $"Reconnected FTP source: {FtpHost}:{FtpPort}{remotePath}";
            }
            catch
            {
                HasLastFtpReconnectFailure = true;
                RefreshUxEmptyStateHints();
                StatusMessage = $"Last FTP source not reachable: {FtpHost}:{FtpPort}{remotePath}";
            }
        }
    }
}
