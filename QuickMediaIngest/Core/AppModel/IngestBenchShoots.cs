#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Shoot list for the ingest-bench (select, skip, keyword filter, transport badge).</summary>
    public static class IngestBenchShoots
    {
        public static TimeSpan Gap { get; } = TimeSpan.FromHours(1);

        public static void Rebuild(IngestBenchHost host) =>
            RebuildFromItems(host, IngestBenchImport.CollectItems(host.ScanRoots, host.Model.ExcludedFolders));

        public static void RebuildFromItems(IngestBenchHost host, List<ImportItem> items)
        {
            CullSelectionPersistence.Snapshot(host.Groups.SelectMany(g => g.Items));
            CullSelectionPersistence.Restore(items);
            IngestBenchWatch.ApplyTimeZone(host, items);
            List<ItemGroup> groups = new GroupBuilder().BuildGroups(items, IngestBenchFilters.Gap(host.Model));
            host.Groups.Clear();
            host.Groups.AddRange(groups);
            ApplyFilter(host);
            IngestBenchAdb.DedupeGroups(host);
            foreach (ItemGroup group in host.Groups)
            {
                PreviewStackApplier.Apply(
                    group.Items,
                    host.Model.Prefs.GroupRawJpeg,
                    host.Model.Prefs.ExpandStacked || group.ExpandStackedPairsInShoot);
            }
            IngestBenchThumbs.Refresh(host);
            host.Model.SelectedShoot = host.Model.Shoots.FirstOrDefault();
            host.Model.SelectedCullItem = host.Model.SelectedShoot?.Items.FirstOrDefault();
        }

        public static void ApplyFilter(IngestBenchHost host)
        {
            string filter = host.Model.ShootFilter?.Trim() ?? string.Empty;
            host.Model.Shoots.Clear();
            foreach (ItemGroup group in host.Groups)
            {
                if (Matches(group, filter) && group.Items.Any(i => IngestBenchFilters.PassesFileType(i, host.Model.FilterFileType)))
                {
                    host.Model.Shoots.Add(group);
                }
            }

            IngestBenchFilters.RefreshChips(host.Model);
        }

        public static void ToggleSelectAll(IngestBenchHost host)
        {
            bool allOn = host.Groups.Count > 0 && host.Groups.All(g => g.IsSelected);
            SelectAll(host, selected: !allOn);
        }

        public static void SelectAll(IngestBenchHost host, bool selected)
        {
            foreach (ItemGroup group in host.Groups)
            {
                group.IsSelected = selected;
            }
        }

        public static void SkipVisibleSelected(IngestBenchHost host)
        {
            foreach (ItemGroup group in host.Model.Shoots)
            {
                if (group.IsSelected)
                {
                    group.IsSelected = false;
                }
            }
        }

        public static void ToggleExpanded(ItemGroup? group)
        {
            if (group is null)
            {
                return;
            }

            group.IsExpanded = !group.IsExpanded;
        }

        public static void SetAllExpanded(IngestBenchHost host, bool expanded)
        {
            foreach (ItemGroup group in host.Groups)
            {
                group.IsExpanded = expanded;
            }
        }

        public static void IgnoreFolder(IngestBenchHost host, ItemGroup? group)
        {
            if (group is null || string.IsNullOrWhiteSpace(group.FolderPath))
            {
                return;
            }

            IngestBenchExclusions.Add(host, group.FolderPath);
        }

        public static List<ItemGroup> SelectedGroups(IngestBenchHost host) =>
            host.Groups.Where(g => g.IsSelected).ToList();

        public static void DropImported(IngestBenchHost host, IEnumerable<ItemGroup> imported, IEnumerable<string> failed)
        {
            var fail = new HashSet<string>(failed, StringComparer.OrdinalIgnoreCase);
            foreach (ItemGroup group in imported.ToArray())
            {
                foreach (ImportItem item in group.Items.ToArray())
                {
                    if (item.IsSelected && !fail.Contains(item.SourcePath))
                    {
                        group.Items.Remove(item);
                    }
                }

                if (group.Items.Count == 0)
                {
                    host.Groups.Remove(group);
                }
            }

            ApplyFilter(host);
            host.Model.NoteSources(host.Groups.Sum(g => g.Items.Count));
            host.Model.PreviewPath = string.Empty;
            host.Model.CompareLeftPath = string.Empty;
            host.Model.CompareRightPath = string.Empty;
            host.Model.ShowCompare = false;
            host.Model.SelectedCullItem = null;
            host.Model.SelectedShoot = host.Model.Shoots.FirstOrDefault();
        }

        private static bool Matches(ItemGroup group, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            return group.Title.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || group.KeywordsText.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || group.Items.Any(i => i.FileName.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }
    }
}
