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
        partial void OnFilterStartDateChanged(DateTime? value)
        {
            EnsureFilteredItemsViewSource();
            ApplyFiltersToCurrentGroups();
            SyncActiveFilterChips();
        }
        partial void OnFilterEndDateChanged(DateTime? value)
        {
            EnsureFilteredItemsViewSource();
            ApplyFiltersToCurrentGroups();
            SyncActiveFilterChips();
        }
        partial void OnFilterFileTypeChanged(string value)
        {
            EnsureFilteredItemsViewSource();
            ApplyFiltersToCurrentGroups();
            SyncActiveFilterChips();
        }
        partial void OnFilterKeywordChanged(string value)
        {
            EnsureFilteredItemsViewSource();
            ApplyFiltersToCurrentGroups();
            SyncActiveFilterChips();
        }

        private void ApplyFiltersToCurrentGroups()
        {
            foreach (var group in Groups)
            {
                ApplyPreviewStacks(group.Items, ExpandPreviewStacks || group.ExpandStackedPairsInShoot, GroupRawAndRenderedPairs);
            }

            _shootFilterService.ApplyToolbarFilters(Groups.ToList(), BuildFilterCriteria());

            RefreshPreviewHealthSummary();
            RefreshUxEmptyStateHints();
        }

        private ShootFilterCriteria BuildFilterCriteria() =>
            new()
            {
                FilterStartDate = FilterStartDate,
                FilterEndDate = FilterEndDate,
                FilterKeyword = FilterKeyword ?? string.Empty,
                FilterFileType = FilterFileType ?? string.Empty
            };

        partial void OnHasUnifiedFtpListingFailuresChanged(bool value) => RefreshUxEmptyStateHints();

        partial void OnHasLastFtpReconnectFailureChanged(bool value) => RefreshUxEmptyStateHints();

        [RelayCommand]
        private void OpenRecycleBin()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "shell:RecycleBinFolder",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Open Recycle Bin failed.");
            }
        }

        [RelayCommand]
        private void DismissPostDeleteRecoveryBanner() => ShowPostDeleteRecoveryBanner = false;

        [RelayCommand]
        private void DismissFtpProblemHints()
        {
            HasUnifiedFtpListingFailures = false;
            HasLastFtpReconnectFailure = false;
            RefreshUxEmptyStateHints();
        }

        private void AddNotificationFeedEntry(string? message, bool useSuccessAccent = false, bool isSessionDivider = false)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            void InsertEntry()
            {
                string line = $"{DateTime.Now:HH:mm:ss} - {message.Trim()}";
                if (!isSessionDivider &&
                    NotificationFeed.Count > 0 &&
                    string.Equals(NotificationFeed[0].DisplayText, line, StringComparison.Ordinal))
                {
                    return;
                }

                NotificationFeed.Insert(0, new NotificationFeedLine
                {
                    DisplayText = line,
                    UseSuccessAccent = useSuccessAccent,
                    IsSessionDivider = isSessionDivider
                });
                const int maxFeedEntries = 200;
                while (NotificationFeed.Count > maxFeedEntries)
                {
                    NotificationFeed.RemoveAt(NotificationFeed.Count - 1);
                }
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                return;
            }

            if (dispatcher.CheckAccess())
            {
                InsertEntry();
            }
            else
            {
                dispatcher.Invoke(InsertEntry);
            }
        }

        private void ExecuteCopySkippedFoldersReport()
        {
            if (string.IsNullOrWhiteSpace(SkippedFoldersReportText))
            {
                return;
            }

            try
            {
                Clipboard.SetText(SkippedFoldersReportText);
                StatusMessage = AppLocalizer.Get("Vm_Status_SkippedFolderCopied");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Unable to copy report: {ex.Message}";
            }
        }

        private void ExecuteCloseSkippedFoldersReport()
        {
            ShowSkippedFoldersDialog = false;
        }

        /// <summary>Escape / Cancel: closes the topmost in-app overlay. Does not cancel an in-progress import (only its dialog is tied to import completion).</summary>
        private void DismissTopOverlay()
        {
            if (ShowScanExclusionsPanel)
            {
                ShowScanExclusionsPanel = false;
                return;
            }

            if (ShowSettingsDialog)
            {
                ShowSettingsDialog = false;
                return;
            }

            if (ShowImportHistoryDialog)
            {
                ShowImportHistoryDialog = false;
                return;
            }

            if (ShowScanProgressDialog)
            {
                ShowScanProgressDialog = false;
                return;
            }

            if (ShowDriveSelectionDialog)
            {
                ShowDriveSelectionDialog = false;
                return;
            }

            if (ShowAddFtpDialog)
            {
                ShowAddFtpDialog = false;
                return;
            }

            if (ShowAboutDialog)
            {
                ShowAboutDialog = false;
                return;
            }

            if (ShowSkippedFoldersDialog)
            {
                ExecuteCloseSkippedFoldersReport();
            }
        }

        private void SyncActiveFilterChips()
        {
            ActiveFilterChips.Clear();
            if (!string.IsNullOrWhiteSpace(FilterKeyword))
            {
                ActiveFilterChips.Add(new FilterChipViewModel { Id = "keyword", Label = AppLocalizer.Format("Vm_FilterChip_KeywordLabel", FilterKeyword) });
            }

            if (!string.IsNullOrWhiteSpace(FilterFileType))
            {
                ActiveFilterChips.Add(new FilterChipViewModel { Id = "type", Label = AppLocalizer.Format("Vm_FilterChip_TypeLabel", FilterFileTypeLocalization.GetDisplayLabel(FilterFileType)) });
            }

            if (FilterStartDate.HasValue)
            {
                ActiveFilterChips.Add(new FilterChipViewModel { Id = "start", Label = AppLocalizer.Format("Vm_FilterChip_DateFromLabel", FilterStartDate.Value.ToString("d", CultureInfo.CurrentCulture)) });
            }

            if (FilterEndDate.HasValue)
            {
                ActiveFilterChips.Add(new FilterChipViewModel { Id = "end", Label = AppLocalizer.Format("Vm_FilterChip_DateToLabel", FilterEndDate.Value.ToString("d", CultureInfo.CurrentCulture)) });
            }
        }

        [RelayCommand]
        private void RemoveFilterChip(string? chipId)
        {
            switch (chipId)
            {
                case "keyword":
                    FilterKeyword = string.Empty;
                    break;
                case "type":
                    FilterFileType = string.Empty;
                    break;
                case "start":
                    FilterStartDate = null;
                    break;
                case "end":
                    FilterEndDate = null;
                    break;
                default:
                    return;
            }
        }

        private void RefreshPreviewHealthSummary()
        {
            try
            {
                int loaded = 0;
                int failed = 0;
                int missing = 0;

                foreach (var group in Groups)
                {
                    foreach (var item in group.Items.Where(i => i.IsPreviewVisible))
                    {
                        switch (item.ThumbnailPreviewStatus)
                        {
                            case ThumbnailPreviewStatus.Loaded:
                                loaded++;
                                break;
                            case ThumbnailPreviewStatus.Failed:
                                failed++;
                                break;
                            default:
                                missing++;
                                break;
                        }
                    }
                }

                PreviewHealthSummary = AppLocalizer.Format("Vm_PreviewHealth", loaded, failed, missing);
            }
            catch
            {
                PreviewHealthSummary = string.Empty;
            }
        }

        private void RefreshImportReadinessSummary()
        {
            try
            {
                int selected = Groups.Sum(g => g.Items.Count(i => i.IsSelected));
                string dest = string.IsNullOrWhiteSpace(DestinationRoot) ? "(not set)" : DestinationRoot;
                int shootsWithKeywords = 0;
                foreach (var g in Groups)
                {
                    if (!g.Items.Any(i => i.IsSelected))
                    {
                        continue;
                    }

                    if (KeywordInputParser.Parse(g.KeywordsText).Count > 0)
                    {
                        shootsWithKeywords++;
                    }
                }

                string kw = !EmbedKeywordsOnImport
                    ? AppLocalizer.Get("Vm_Readiness_KwOff")
                    : AppLocalizer.Format("Vm_Readiness_KwOn", shootsWithKeywords);

                ImportReadinessSummary = AppLocalizer.Format(
                    "Vm_Readiness_Line",
                    selected,
                    dest,
                    DuplicatePolicy,
                    VerificationMode,
                    DeleteAfterImport ? AppLocalizer.Get("Vm_Yes") : AppLocalizer.Get("Vm_No"),
                    kw);

                SelectedFilesStatusLine = $"Files selected: {selected}";
                DestinationStatusLine = $"Save destination: {dest}";
                DeleteAfterImportStatusLine = $"Delete after import: {(DeleteAfterImport ? "On" : "Off")}";
                KeywordsStatusLine = $"Keywords: {kw}";
            }
            catch
            {
                ImportReadinessSummary = string.Empty;
                SelectedFilesStatusLine = string.Empty;
                DestinationStatusLine = string.Empty;
                DeleteAfterImportStatusLine = string.Empty;
                KeywordsStatusLine = string.Empty;
            }
        }
    }
}
