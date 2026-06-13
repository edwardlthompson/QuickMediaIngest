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

        private void RunPostImportActions(List<ItemGroup> selectedGroups)
        {
            try
            {
                string destRoot = DestinationRoot;
                if (OpenDestinationFolderWhenImportCompletes && !string.IsNullOrWhiteSpace(destRoot) && Directory.Exists(destRoot))
                {
                    _ = Task.Run(() =>
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = destRoot,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Could not open destination folder in Explorer.");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Post-import open destination folder step failed.");
            }

            try
            {
                if (SelectedSource is string drive && drive.Length >= 2 && drive[1] == ':')
                {
                    var driveLetter = drive.TrimEnd('\\');
                    var query = $"SELECT * FROM Win32_Volume WHERE DriveLetter = '{driveLetter}'";
                    using var searcher = new System.Management.ManagementObjectSearcher(query);
                    foreach (System.Management.ManagementObject volume in searcher.Get())
                    {
                        try
                        {
                            volume.InvokeMethod("Dismount", null);
                            volume.InvokeMethod("Remove", null);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "WMI dismount/remove failed for volume.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Post-import eject step failed.");
            }

            try
            {
                foreach (var group in selectedGroups)
                {
                    var selectedItems = group.Items.Where(i => i.IsSelected).ToList();
                    if (selectedItems.Count == 0) continue;
                    string folderName = _groupBuilder.GetTargetFolderName(group);
                    string targetDir = Path.Combine(DestinationRoot, folderName);
                    if (!Directory.Exists(targetDir)) continue;
                    var album = new
                    {
                        GroupTitle = group.Title,
                        StartDate = group.StartDate,
                        EndDate = group.EndDate,
                        Items = selectedItems.Select(i => new
                        {
                            i.FileName,
                            i.SourcePath,
                            i.FileSize,
                            i.DateTaken
                        }).ToList()
                    };
                    string json = System.Text.Json.JsonSerializer.Serialize(album, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(Path.Combine(targetDir, "album.json"), json);

                    if (!EmbedKeywordsOnImport)
                    {
                        foreach (var item in selectedItems)
                        {
                            string xmpPath = Path.Combine(targetDir, Path.GetFileNameWithoutExtension(item.FileName) + ".xmp");
                            string xmp = $@"<?xpacket begin='﻿' id='W5M0MpCehiHzreSzNTczkc9d'?>
<x:xmpmeta xmlns:x='adobe:ns:meta/'>
  <rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'>
    <rdf:Description rdf:about=''
      xmlns:dc='http://purl.org/dc/elements/1.1/'
      xmlns:xmp='http://ns.adobe.com/xap/1.0/'
      xmlns:photoshop='http://ns.adobe.com/photoshop/1.0/'>
      <dc:title><rdf:Alt><rdf:li xml:lang='x-default'>{System.Security.SecurityElement.Escape(group.Title)}</rdf:li></rdf:Alt></dc:title>
      <xmp:CreateDate>{item.DateTaken:yyyy-MM-ddTHH:mm:ssZ}</xmp:CreateDate>
      <photoshop:DateCreated>{item.DateTaken:yyyy-MM-ddTHH:mm:ssZ}</photoshop:DateCreated>
      <dc:format>{System.Security.SecurityElement.Escape(item.FileType)}</dc:format>
      <dc:identifier>{System.Security.SecurityElement.Escape(item.FileName)}</dc:identifier>
    </rdf:Description>
  </rdf:RDF>
</x:xmpmeta>
<?xpacket end='w'?>";
                            File.WriteAllText(xmpPath, xmp);
                        }
                    }
                }
            }
            catch
            {
                // Ignore album export errors.
            }
        }

        private void ExecuteImportPreflight()
        {
            SyncStackSelections(Groups);
            var selectedGroups = Groups.Where(g => g.Items.Any(i => i.IsSelected)).ToList();
            int selectedCount = selectedGroups.Sum(g => g.Items.Count(i => i.IsSelected));
            long totalBytes = selectedGroups.SelectMany(g => g.Items).Where(i => i.IsSelected).Sum(i => Math.Max(0, i.FileSize));
            var duplicateNames = selectedGroups
                .SelectMany(g => g.Items.Where(i => i.IsSelected))
                .GroupBy(i => i.FileName, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .Take(25)
                .ToList();

            var report = new
            {
                GeneratedAt = DateTime.Now,
                DestinationRoot,
                SelectedGroups = selectedGroups.Count,
                SelectedFiles = selectedCount,
                TotalBytes = totalBytes,
                DuplicatePolicy,
                VerificationMode,
                PotentialDuplicateNames = duplicateNames
            };

            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest", "preflight");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, $"preflight-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            StatusMessage = AppLocalizer.Format(
                "Vm_Status_PreflightComplete",
                selectedCount,
                totalBytes / (1024d * 1024d),
                path);
        }

        private void ExecuteRetryFailedImports()
        {
            if (FailedImportRecords.Count == 0)
            {
                StatusMessage = AppLocalizer.Get("Vm_Status_NoFailedFilesToRetry");
                return;
            }

            var failedPaths = new HashSet<string>(FailedImportRecords.Select(f => f.SourcePath), StringComparer.OrdinalIgnoreCase);
            foreach (var group in Groups)
            {
                foreach (var item in group.Items)
                {
                    item.IsSelected = failedPaths.Contains(item.SourcePath);
                }
                group.SyncSelectionFromItems();
            }

            StatusMessage = $"Retrying {failedPaths.Count} failed file(s)...";
            ExecuteImport();
        }

        private void ExecuteResumePendingImport()
        {
            try
            {
                string path = GetPendingImportPlanPath();
                if (!File.Exists(path))
                {
                    StatusMessage = AppLocalizer.Get("Vm_Status_NoPendingPlan");
                    return;
                }

                var plan = System.Text.Json.JsonSerializer.Deserialize<PendingImportPlan>(File.ReadAllText(path));
                if (plan == null || plan.SelectedSourcePaths.Count == 0)
                {
                    StatusMessage = AppLocalizer.Get("Vm_Status_EmptyPendingPlan");
                    return;
                }

                if (!string.Equals(plan.SourceId, BuildSelectedSourceId(), StringComparison.OrdinalIgnoreCase))
                {
                    StatusMessage = $"Pending import source is {plan.SourceDisplay}. Select that source first.";
                    return;
                }

                var selectedSet = new HashSet<string>(plan.SelectedSourcePaths, StringComparer.OrdinalIgnoreCase);
                foreach (var group in Groups)
                {
                    foreach (var item in group.Items)
                    {
                        item.IsSelected = selectedSet.Contains(item.SourcePath);
                    }
                    group.SyncSelectionFromItems();
                }

                StatusMessage = $"Resuming pending import ({selectedSet.Count} files).";
                ExecuteImport();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Unable to resume pending import: {ex.Message}";
            }
        }

        private void QueueCurrentImport()
        {
            SyncStackSelections(Groups);
            var selectedPaths = Groups
                .SelectMany(g => g.Items)
                .Where(i => i.IsSelected)
                .Select(i => i.SourcePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (selectedPaths.Count == 0)
            {
                StatusMessage = AppLocalizer.Get("Vm_Status_SelectFilesBeforeQueue");
                return;
            }

            lock (_importQueueLock)
            {
                _importQueue.Enqueue(new QueuedImportJob
                {
                    SourceDisplay = SelectedSource?.ToString() ?? "Unknown",
                    SourceId = BuildSelectedSourceId(),
                    SelectedSourcePaths = selectedPaths
                });
                QueuedImportCount = _importQueue.Count;
            }

            StatusMessage = $"Queued import job with {selectedPaths.Count} file(s).";
            if (!IsImporting)
            {
                TryStartNextQueuedImport();
            }
        }

        private void TryStartNextQueuedImport()
        {
            if (IsImporting)
            {
                return;
            }

            QueuedImportJob? nextJob = null;
            lock (_importQueueLock)
            {
                if (_importQueue.Count > 0)
                {
                    nextJob = _importQueue.Dequeue();
                }
                QueuedImportCount = _importQueue.Count;
            }

            if (nextJob == null)
            {
                return;
            }

            if (!string.Equals(nextJob.SourceId, BuildSelectedSourceId(), StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = $"Queued import for {nextJob.SourceDisplay} is waiting. Switch to that source and queue again to run.";
                return;
            }

            var selectedSet = new HashSet<string>(nextJob.SelectedSourcePaths, StringComparer.OrdinalIgnoreCase);
            foreach (var group in Groups)
            {
                foreach (var item in group.Items)
                {
                    item.IsSelected = selectedSet.Contains(item.SourcePath);
                }
                group.SyncSelectionFromItems();
            }

            StatusMessage = $"Starting queued import ({selectedSet.Count} file(s)).";
            ExecuteImport();
        }

        private void SaveCurrentPreset()
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest", "presets");
                Directory.CreateDirectory(dir);
                var preset = new UserPreset
                {
                    Name = $"preset-{DateTime.Now:yyyyMMdd-HHmmss}",
                    DestinationRoot = DestinationRoot,
                    NamingTemplate = NamingTemplate,
                    VerificationMode = VerificationMode,
                    DuplicatePolicy = DuplicatePolicy,
                    ThumbnailPerformanceMode = ThumbnailPerformanceMode,
                    GroupRawAndRenderedPairs = GroupRawAndRenderedPairs,
                    ExpandPreviewStacks = ExpandPreviewStacks,
                    EmbedKeywordsOnImport = EmbedKeywordsOnImport,
                    ConfirmBeforeImport = ConfirmBeforeImport
                };
                string path = Path.Combine(dir, preset.Name + ".json");
                File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(preset, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                StatusMessage = $"Saved preset: {preset.Name}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Unable to save preset: {ex.Message}";
            }
        }

        private void LoadLatestPreset()
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest", "presets");
                if (!Directory.Exists(dir))
                {
                    StatusMessage = AppLocalizer.Get("Vm_Status_NoPresets");
                    return;
                }

                string? latest = Directory.GetFiles(dir, "*.json")
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();
                if (string.IsNullOrWhiteSpace(latest))
                {
                    StatusMessage = AppLocalizer.Get("Vm_Status_NoPresets");
                    return;
                }

                var preset = System.Text.Json.JsonSerializer.Deserialize<UserPreset>(File.ReadAllText(latest));
                if (preset == null)
                {
                    StatusMessage = AppLocalizer.Get("Vm_Status_PresetCouldNotLoad");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(preset.DestinationRoot)) DestinationRoot = preset.DestinationRoot;
                if (!string.IsNullOrWhiteSpace(preset.NamingTemplate)) NamingTemplate = preset.NamingTemplate;
                if (!string.IsNullOrWhiteSpace(preset.VerificationMode)) VerificationMode = preset.VerificationMode;
                if (!string.IsNullOrWhiteSpace(preset.DuplicatePolicy)) DuplicatePolicy = preset.DuplicatePolicy;
                if (!string.IsNullOrWhiteSpace(preset.ThumbnailPerformanceMode)) ThumbnailPerformanceMode = preset.ThumbnailPerformanceMode;
                GroupRawAndRenderedPairs = preset.GroupRawAndRenderedPairs;
                ExpandPreviewStacks = preset.ExpandPreviewStacks;
                EmbedKeywordsOnImport = preset.EmbedKeywordsOnImport;
                ConfirmBeforeImport = preset.ConfirmBeforeImport;
                SaveConfig();
                StatusMessage = $"Loaded preset: {preset.Name}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Unable to load preset: {ex.Message}";
            }
        }
    }
}
