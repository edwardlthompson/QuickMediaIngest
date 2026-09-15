#nullable enable
using System;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Watch-folder, split/merge, timezone, batch rename for the ingest-bench.</summary>
    public static class IngestBenchWatch
    {
        public static void Attach(IngestBenchHost host)
        {
            host.FolderWatch?.Dispose();
            host.FolderWatch = new WatchFolderService(NullLogger<WatchFolderService>.Instance);
            host.FolderWatch.FileDetected += (_, path) =>
            {
                if (!string.IsNullOrWhiteSpace(path) && FileLooksLikeMedia(path))
                {
                    host.Post(() => host.RefreshScan());
                }
            };
            ApplyWatchFolder(host);
        }

        public static void Detach(IngestBenchHost host)
        {
            host.FolderWatch?.Dispose();
            host.FolderWatch = null;
        }

        public static void ApplyWatchFolder(IngestBenchHost host)
        {
            string path = host.Model.Prefs.WatchFolder?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(path))
            {
                host.FolderWatch?.StopWatching();
                return;
            }

            host.FolderWatch?.StartWatching(path);
        }

        public static void ApplyTimeZone(IngestBenchHost host) =>
            ApplyTimeZone(host, host.Groups.SelectMany(g => g.Items));

        public static void ApplyTimeZone(IngestBenchHost host, System.Collections.Generic.IEnumerable<ImportItem> items)
        {
            if (!Enum.TryParse(host.Model.Prefs.TimeZoneMode, ignoreCase: true, out TimeZoneOverrideMode mode))
            {
                mode = TimeZoneOverrideMode.CameraAsIs;
            }

            DateTimeZoneAdjuster.ApplyTimeZoneOverride(items, mode);
        }

        public static bool SplitSelected(IngestBenchHost host)
        {
            ItemGroup? group = host.Model.SelectedShoot;
            if (group is null || group.Items.Count < 2)
            {
                return false;
            }

            int index = Math.Max(1, group.Items.Count / 2);
            try
            {
                var (_, secondary) = GroupBuilder.SplitGroup(group, index, group.Title + " B");
                int at = host.Groups.IndexOf(group);
                host.Groups.Insert(at < 0 ? host.Groups.Count : at + 1, secondary);
                IngestBenchShoots.ApplyFilter(host);
                host.Model.SelectedShoot = group;
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public static bool MergeSelected(IngestBenchHost host)
        {
            ItemGroup? primary = host.Model.SelectedShoot;
            if (primary is null)
            {
                return false;
            }

            ItemGroup? other = host.Groups.FirstOrDefault(g => g != primary && g.IsSelected)
                ?? NextGroup(host, primary);
            if (other is null)
            {
                return false;
            }

            GroupBuilder.MergeGroups(primary, other);
            host.Groups.Remove(other);
            IngestBenchShoots.ApplyFilter(host);
            host.Model.SelectedShoot = primary;
            return true;
        }

        public static void RenameVisible(IngestBenchHost host)
        {
            string baseTitle = string.IsNullOrWhiteSpace(host.Model.Prefs.BatchRenameBase)
                ? "Shoot"
                : host.Model.Prefs.BatchRenameBase.Trim();
            ShootTitleBatchRenamer.RenameShootsWithUniqueness(host.Model.Shoots, baseTitle);
        }

        private static ItemGroup? NextGroup(IngestBenchHost host, ItemGroup current)
        {
            int at = host.Groups.IndexOf(current);
            return at >= 0 && at + 1 < host.Groups.Count ? host.Groups[at + 1] : null;
        }

        private static bool FileLooksLikeMedia(string path)
        {
            try
            {
                return MediaExtensions.IsMediaFile(path);
            }
            catch
            {
                return false;
            }
        }
    }
}
