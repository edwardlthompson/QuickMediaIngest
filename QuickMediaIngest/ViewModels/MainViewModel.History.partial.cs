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

        private void SetAllShootsSelected(bool isSelected)
        {
            _isUpdatingSelectAll = true;
            try
            {
                foreach (var group in Groups)
                {
                    group.IsSelected = isSelected;
                }

                _selectAll = isSelected;
                OnPropertyChanged(nameof(SelectAll));
            }
            finally
            {
                _isUpdatingSelectAll = false;
            }
        }

        private void Group_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ItemGroup.IsSelected))
            {
                if (_isUpdatingSelectAll) return;
                UpdateSelectAllFromGroups();
            }

            if (e.PropertyName == nameof(ItemGroup.KeywordsText))
            {
                RefreshImportReadinessSummary();
            }

            if (e.PropertyName == nameof(ItemGroup.IsExpanded))
            {
                if (_isBulkUpdatingGroupExpansion)
                {
                    return;
                }

                bool allExpanded = Groups.Count > 0 && Groups.All(g => g.IsExpanded);
                if (AllGroupsExpanded != allExpanded)
                {
                    AllGroupsExpanded = allExpanded;
                }
            }
        }

        private void UpdateSelectAllFromGroups()
        {
            if (Groups.Count == 0)
            {
                return;
            }

            bool allSelected = Groups.All(g => g.IsSelected);
            if (_selectAll == allSelected)
            {
                return;
            }

            _isUpdatingSelectAll = true;
            try
            {
                _selectAll = allSelected;
                OnPropertyChanged(nameof(SelectAll));
            }
            finally
            {
                _isUpdatingSelectAll = false;
            }
        }

        private void RebuildGroupsFromCurrentItems()
        {
            GroupsListRebuildStarting?.Invoke(this, EventArgs.Empty);

            foreach (var existing in Groups)
            {
                existing.PropertyChanged -= Group_PropertyChanged;
            }

            DetachImportItemSelectionHandlers();

            Groups.Clear();
            EnsureFilteredItemsViewSource();


            if (_currentSourceItems.Count == 0)
            {
                GroupsListRebuildCompleted?.Invoke(this, EventArgs.Empty);
                return;
            }


            var groups = _groupBuilder.BuildGroups(_currentSourceItems, TimeSpan.FromHours(TimeBetweenShootsHours));


            foreach (var group in groups)
            {
                if (group.Items.Count == 0)
                {
                    continue;
                }

                group.AlbumName = AlbumName;
                group.FolderPath = group.Items[0].IsFtpSource
                    ? ExtractFtpFolderPath(group.Items[0].SourcePath)
                    : (Path.GetDirectoryName(group.Items[0].SourcePath) ?? string.Empty);
                group.SyncSelectionFromItems();
                ApplyPreviewStacks(group.Items, ExpandPreviewStacks || group.ExpandStackedPairsInShoot, GroupRawAndRenderedPairs);
                string expandKey = BuildShootExpansionKey(group);
                if (_shootGroupExpandedMemory.TryGetValue(expandKey, out bool remembered))
                {
                    group.IsExpanded = remembered;
                }

                foreach (var item in group.Items)
                {
                    string key = BuildItemKey(item);
                    if (item.Thumbnail == null && _thumbnailByItemKey.TryGetValue(key, out var cachedThumb))
                    {
                        item.Thumbnail = cachedThumb;
                        item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Loaded;
                    }
                }
                AttachImportItemSelectionHandlers(group);
                group.PropertyChanged += Group_PropertyChanged;
                Groups.Add(group);
            }


            UpdateSelectAllFromGroups();
            AllGroupsExpanded = Groups.Count > 0 && Groups.All(g => g.IsExpanded);
            ApplyFiltersToCurrentGroups();
            RefreshImportReadinessSummary();
            SyncActiveFilterChips();
            RefreshPreviewHealthSummary();
            RefreshUxEmptyStateHints();
            StatusMessage = $"Updated folder separation to {TimeBetweenShootsHours} hour{(TimeBetweenShootsHours == 1 ? string.Empty : "s")}.";
            GroupsListRebuildCompleted?.Invoke(this, EventArgs.Empty);
        }

        private static readonly HashSet<string> RenderedPreviewExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".heic", ".heif", ".png", ".webp", ".tif", ".tiff"
        };

        private static readonly HashSet<string> RawPreviewExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".dng", ".cr2", ".cr3", ".nef", ".arw", ".raf", ".orf", ".rw2", ".srw"
        };

        private static void ApplyPreviewStacks(List<ImportItem> items, bool expandPreviewStacks, bool groupRawAndRenderedPairs)
        {
            foreach (var item in items)
            {
                item.IsPreviewVisible = true;
                item.IsStackRepresentative = true;
                item.StackKey = item.SourcePath;
                item.PreviewLabel = item.FileName;
            }

            if (!groupRawAndRenderedPairs)
            {
                return;
            }

            var imageItems = items.Where(i => !i.IsVideo).ToList();
            var groups = imageItems.GroupBy(i => Path.GetFileNameWithoutExtension(i.FileName), StringComparer.OrdinalIgnoreCase);
            foreach (var group in groups)
            {
                var members = group.ToList();
                if (members.Count <= 1)
                {
                    continue;
                }

                var rendered = members.Where(m => RenderedPreviewExtensions.Contains(Path.GetExtension(m.FileName))).ToList();
                var raws = members.Where(m => RawPreviewExtensions.Contains(Path.GetExtension(m.FileName))).ToList();
                if (rendered.Count == 0 || raws.Count == 0)
                {
                    continue;
                }

                var representative = rendered
                    .OrderBy(m => Path.GetExtension(m.FileName).Equals(".jpg", StringComparison.OrdinalIgnoreCase) ? 0 :
                                  Path.GetExtension(m.FileName).Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ? 1 :
                                  Path.GetExtension(m.FileName).Equals(".heic", StringComparison.OrdinalIgnoreCase) ? 2 : 3)
                    .First();

                string stackKey = group.Key;
                int hiddenCount = members.Count - 1;
                foreach (var member in members)
                {
                    member.StackKey = stackKey;
                    member.IsStackRepresentative = ReferenceEquals(member, representative);
                    member.IsPreviewVisible = expandPreviewStacks || member.IsStackRepresentative;
                    member.PreviewLabel = member.IsStackRepresentative && hiddenCount > 0
                        ? $"{member.FileName} (+{hiddenCount})"
                        : member.FileName;
                }
            }
        }

        private static void SyncStackSelections(IEnumerable<ItemGroup> groups)
        {
            foreach (var group in groups)
            {
                var stackGroups = group.Items
                    .GroupBy(i => i.StackKey, StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1);

                foreach (var stack in stackGroups)
                {
                    var leader = stack.FirstOrDefault(i => i.IsStackRepresentative) ?? stack.First();
                    bool selected = leader.IsSelected;
                    foreach (var member in stack)
                    {
                        member.IsSelected = selected;
                    }
                }
            }
        }

        private static void ClearThumbnailDiskCache()
        {
            try
            {
                string cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest", "Thumbnails");
                if (Directory.Exists(cacheDir))
                {
                    Directory.Delete(cacheDir, true);
                }
            }
            catch
            {
                // Ignore cache purge failures.
            }
        }

        private void MaybeShowSkippedFoldersScanReport(string sourceLabel, HashSet<string> ftpListingFailures, HashSet<string> userExcludedFolders)
        {
            int ftpCount = ftpListingFailures.Count;
            int excludedCount = userExcludedFolders.Count;
            if (ftpCount == 0 && excludedCount == 0)
            {
                return;
            }

            if (ftpCount == 0 && excludedCount > 0 && SuppressExcludedFolderScanReminders)
            {
                StatusMessage = AppLocalizer.Format("Vm_Status_ScanExcludedFoldersOnlySummary", excludedCount);
                return;
            }

            var ftpOrdered = ftpListingFailures.OrderBy(s => s).ToList();
            var excludedOrdered = userExcludedFolders.OrderBy(s => s).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            ShowSkippedFoldersSuppressReminderOption = ftpCount == 0 && excludedCount > 0;

            const int maxToShow = 15;
            static string FormatList(IReadOnlyList<string> lines, int max)
            {
                var shown = lines.Take(max).ToList();
                string tail = lines.Count > max ? $"\n...and {lines.Count - max} more." : string.Empty;
                return string.Join("\n", shown) + tail;
            }

            var sections = new List<string>();
            if (excludedOrdered.Count > 0)
            {
                sections.Add(AppLocalizer.Format("Vm_SkippedScan_SectionExcluded", FormatList(excludedOrdered, maxToShow)));
            }

            if (ftpOrdered.Count > 0)
            {
                sections.Add(AppLocalizer.Format("Vm_SkippedScan_SectionFtp", FormatList(ftpOrdered, maxToShow)));
                sections.Add(AppLocalizer.Get("Vm_SkippedScan_FtpTip"));
            }

            string message = string.Join("\n\n", sections.Where(s => !string.IsNullOrWhiteSpace(s)));

            string title;
            if (ftpOrdered.Count > 0 && excludedOrdered.Count > 0)
            {
                title = AppLocalizer.Format("Vm_SkippedScan_TitleCombined", sourceLabel);
            }
            else if (ftpOrdered.Count > 0)
            {
                title = AppLocalizer.Format("Vm_SkippedScan_TitleFtpOnly", ftpOrdered.Count);
            }
            else
            {
                title = AppLocalizer.Format("Vm_SkippedScan_TitleExcludedOnly", excludedOrdered.Count);
            }

            StatusMessage = ftpOrdered.Count > 0
                ? AppLocalizer.Format("Vm_Status_ScanSummaryWithFtpIssues", excludedOrdered.Count, ftpOrdered.Count)
                : AppLocalizer.Format("Vm_Status_ScanSummaryExcludedOnly", excludedOrdered.Count);

            SkippedFoldersReportTitle = title;
            SkippedFoldersReportText = message;
            ShowSkippedFoldersDialog = true;
        }

        // Ensures the filtered items view source is set up and refreshed for filtering/search
        private void EnsureFilteredItemsViewSource()
        {
            // Build a flat list of all ImportItems from all groups
            var allItems = Groups.SelectMany(g => g.Items).ToList();

            // Update AvailableFileTypes
            var fileTypes = allItems.Select(i => i.FileType).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t => t).ToList();
            AvailableFileTypes.Clear();
            AvailableFileTypes.Add(string.Empty);
            AvailableFileTypes.Add(FilterFileTypeLocalization.AllMedia);
            AvailableFileTypes.Add(FilterFileTypeLocalization.Images);
            AvailableFileTypes.Add(FilterFileTypeLocalization.Videos);
            AvailableFileTypes.Add(FilterFileTypeLocalization.Raw);
            AvailableFileTypes.Add(FilterFileTypeLocalization.Jpeg);
            foreach (var t in fileTypes)
                AvailableFileTypes.Add(t);

            // Set up the CollectionView for filtering
            var cvs = System.Windows.Data.CollectionViewSource.GetDefaultView(allItems);
            var criteria = BuildFilterCriteria();
            cvs.Filter = o =>
                o is ImportItem item && _shootFilterService.PassesToolbarRules(item, criteria);
            FilteredItemsView = cvs;
            cvs.Refresh();
        }
    }
}
