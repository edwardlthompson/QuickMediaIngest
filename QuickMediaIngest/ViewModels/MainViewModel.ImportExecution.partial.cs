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

        private static void ShowWindowsImportCompletionNotification(int importedCount, int totalCount, int failedCount)
        {
            string title = failedCount > 0
                ? AppLocalizer.Get("Msg_ImportComplete_Title_Warning")
                : AppLocalizer.Get("Msg_ImportComplete_Title_Success");
            string body = failedCount > 0
                ? AppLocalizer.Format("Msg_ImportComplete_Body_Warning", importedCount, totalCount, failedCount)
                : AppLocalizer.Format("Msg_ImportComplete_Body_Success", importedCount, totalCount);

            try
            {
                SystemSounds.Exclamation.Play();
            }
            catch
            {
                // Ignore local sound playback issues.
            }

            try
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show(
                        body,
                        title,
                        MessageBoxButton.OK,
                        failedCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
                }));
            }
            catch
            {
                // Ignore notification failures to avoid interrupting import completion.
            }
        }

        private IngestOptions CreateIngestOptions(ItemGroup group)
        {
            DuplicateHandlingMode duplicateMode = DuplicatePolicy switch
            {
                "Skip" => DuplicateHandlingMode.Skip,
                "OverwriteIfNewer" => DuplicateHandlingMode.OverwriteIfNewer,
                _ => DuplicateHandlingMode.Suffix
            };

            ImportVerificationMode verification = VerificationMode == "Strict"
                ? ImportVerificationMode.Strict
                : ImportVerificationMode.Fast;

            List<string> keywords = KeywordInputParser.Parse(group.KeywordsText);
            bool applyKeywords = EmbedKeywordsOnImport && keywords.Count > 0;

            int maxCopy = ImportSingleThreaded ? 1 : 0;
            string? localSamplePath = group.Items
                .FirstOrDefault(i => i.IsSelected && !i.IsFtpSource)
                ?.SourcePath;
            maxCopy = RemovableDriveIo.CapConcurrentCopies(maxCopy, localSamplePath);
            int delayMs = Math.Max(0, ImportCooldownBetweenFilesMs);

            return new IngestOptions
            {
                DuplicateHandling = duplicateMode,
                VerificationMode = verification,
                ApplyImportKeywords = applyKeywords,
                ImportKeywords = applyKeywords ? keywords : null,
                MaxConcurrentFileCopies = maxCopy,
                DelayBetweenFilesMilliseconds = delayMs,
                ByteProgressTracker = _importByteProgressTracker,
            };
        }

        private async Task ExecuteUnifiedImportAsync(List<ItemGroup> selectedGroups, CancellationToken cancellationToken)
        {
            IFileProvider localProvider = _fileProviderFactory.CreateLocalProvider();
            var ftpProviders = new Dictionary<string, IFileProvider>(StringComparer.OrdinalIgnoreCase);
            var ftpSourcesByKey = Sources
                .OfType<FtpSourceItem>()
                .ToDictionary(BuildSourceKey, ftp => ftp, StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (var group in selectedGroups)
                {
                    var localItems = group.Items.Where(i => i.IsSelected && !i.IsFtpSource).ToList();
                    if (localItems.Count > 0)
                    {
                        await ImportUnifiedSubsetAsync(group, localItems, localProvider, cancellationToken);
                    }

                    var ftpBatches = group.Items
                        .Where(i => i.IsSelected && i.IsFtpSource)
                        .GroupBy(i => i.SourceId, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    foreach (var ftpBatch in ftpBatches)
                    {
                        if (!ftpSourcesByKey.TryGetValue(ftpBatch.Key, out var ftpSource))
                        {
                            foreach (var item in ftpBatch)
                            {
                                RegisterManualImportFailure(item, "Missing FTP source configuration.");
                            }

                            continue;
                        }

                        if (!ftpProviders.TryGetValue(ftpBatch.Key, out var ftpProvider))
                        {
                            ftpProvider = _fileProviderFactory.CreateFtpProvider(ftpSource.Host, ftpSource.Port, ftpSource.User, ftpSource.Pass);
                            ftpProviders[ftpBatch.Key] = ftpProvider;
                        }

                        await ImportUnifiedSubsetAsync(group, ftpBatch.ToList(), ftpProvider, cancellationToken);
                    }
                }
            }
            finally
            {
                foreach (var provider in ftpProviders.Values)
                {
                    if (provider is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                }
            }
        }

        private async Task ImportUnifiedSubsetAsync(ItemGroup group, List<ImportItem> items, IFileProvider provider, CancellationToken cancellationToken)
        {
            if (items.Count == 0)
            {
                return;
            }

            var subsetGroup = new ItemGroup
            {
                Title = group.Title,
                StartDate = group.StartDate,
                EndDate = group.EndDate,
                AlbumName = group.AlbumName,
                FolderPath = group.FolderPath,
                KeywordsText = group.KeywordsText,
                Items = items
            };

            var engine = _ingestEngineFactory.Create(provider);
            WireIngestEngineProgress(engine);

            await engine.IngestGroupAsync(
                subsetGroup,
                DestinationRoot,
                NamingTemplate,
                cancellationToken,
                CreateIngestOptions(group),
                DeleteAfterImport);
        }

        // ExecuteBrowseDestination removed along with its UI entry.

        private void ExecuteBrowseScanPath()
        {
            if (SelectedSource is not string localRoot)
            {
                StatusMessage = AppLocalizer.Get("Vm_Status_BrowseOnlyLocal");
                return;
            }

            string initialDirectory = ResolveLocalScanPath(localRoot, ScanPath);
            if (!Directory.Exists(initialDirectory))
            {
                initialDirectory = localRoot;
            }

            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = AppLocalizer.Get("Dlg_SelectFolderToScan"),
                InitialDirectory = initialDirectory
            };

            if (dialog.ShowDialog() == true)
            {
                ScanPath = dialog.FolderName;
            }
        }

        private void OpenDriveSelectionDialog()
        {
            DriveSelectionItems.Clear();
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady && (d.DriveType == DriveType.Removable || d.DriveType == DriveType.Fixed))
                .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var drive in drives)
            {
                string deviceId = GetOrCreateDeviceIdForDrive(drive);
                _driveDeviceIdByPath[drive.Name] = deviceId;
                _drivePathByDeviceId[deviceId] = drive.Name;
                bool selected = _selectedDriveDeviceIds.Count == 0
                    ? drive.DriveType == DriveType.Removable
                    : _selectedDriveDeviceIds.Contains(deviceId);
                DriveSelectionItems.Add(new DriveSelectionOption
                {
                    Name = drive.Name,
                    Label = $"{drive.Name} ({drive.DriveType})",
                    DriveType = drive.DriveType.ToString(),
                    DeviceId = deviceId,
                    IsSelected = selected
                });
            }

            ShowDriveSelectionDialog = true;
        }

        private async Task ExecuteConfirmDriveSelectionAsync()
        {
            _selectedDriveDeviceIds.Clear();
            foreach (var item in DriveSelectionItems.Where(i => i.IsSelected))
            {
                if (!string.IsNullOrWhiteSpace(item.DeviceId))
                {
                    _selectedDriveDeviceIds.Add(item.DeviceId);
                }
            }

            ShowDriveSelectionDialog = false;
            await ScanDrivesAsync();
            await RefreshExclusionManagementListsAsync().ConfigureAwait(true);
            SaveConfig();
        }

        /// <summary>
        /// Skipped-folder rules use the same keys as <see cref="ApplySkippedFolderFilters"/> (not the literal Unified sentinel).
        /// </summary>
        private string ResolveSkipRuleSourceId(string folderPath)
        {
            if (SelectedSource is not UnifiedSourceItem)
            {
                return BuildSelectedSourceId();
            }

            ItemGroup? group = Groups.FirstOrDefault(g =>
                string.Equals(g.FolderPath, folderPath, StringComparison.OrdinalIgnoreCase));
            ImportItem? item = group?.Items.FirstOrDefault();
            if (item == null)
            {
                _logger.LogWarning("Skip folder: no group matching path {Path}.", folderPath);
                return string.Empty;
            }

            if (item.IsFtpSource)
            {
                return item.SourceId;
            }

            string root = Path.GetPathRoot(item.SourcePath) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(root))
            {
                return string.Empty;
            }

            return BuildLocalSourceRuleKey(root);
        }

        private void ExecuteSkipFolder(string? folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                _logger.LogWarning("Skip folder invoked with empty path (UI binding may be broken).");
                return;
            }

            ItemGroup? matchGroup = Groups.FirstOrDefault(g =>
                string.Equals(g.FolderPath, folderPath, StringComparison.OrdinalIgnoreCase));
            bool isFtpFolder = matchGroup?.Items.FirstOrDefault()?.IsFtpSource == true;
            string normalizedFolder = isFtpFolder ? NormalizeFtpPath(folderPath) : folderPath.TrimEnd('\\', '/');

            string sourceId = ResolveSkipRuleSourceId(folderPath);
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                StatusMessage = AppLocalizer.Get("Vm_Status_SkipRuleAttachFailed");
                return;
            }
            if (!_skippedFoldersBySource.TryGetValue(sourceId, out var entries))
            {
                entries = new List<string>();
                _skippedFoldersBySource[sourceId] = entries;
            }

            if (!entries.Any(p => FolderPathsMatchForSkipRule(p, normalizedFolder, isFtpFolder)))
            {
                entries.Add(normalizedFolder);
            }

            InvalidateSourceItemsCache(sourceId);

            SaveConfig();

            var toRemove = Groups.Where(g => FolderPathsMatchForSkipRule(g.FolderPath, normalizedFolder, isFtpFolder)).ToList();
            foreach (var group in toRemove)
            {
                Groups.Remove(group);
            }

            _logger.LogInformation("Skip folder rule stored for source key {SourceId}: {FolderPath}", sourceId, normalizedFolder);

            StatusMessage = $"Skipping folder in future scans: {normalizedFolder}";
            _ = RefreshExclusionManagementListsAsync();
        }
    }
}
