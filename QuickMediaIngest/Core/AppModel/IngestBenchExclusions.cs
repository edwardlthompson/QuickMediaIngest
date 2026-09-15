#nullable enable
using System;
using System.Linq;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Scan-exclusions overlay: add/remove folder prefixes and persist.</summary>
    public static class IngestBenchExclusions
    {
        public static void Load(IngestBenchHost host)
        {
            host.Model.ExcludedFolders.Clear();
            foreach (string folder in new ScanExclusionStore(host.Paths).Load())
            {
                host.Model.ExcludedFolders.Add(folder);
            }
        }

        public static void Add(IngestBenchHost host, string? folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                return;
            }

            string normalized = ScanExclusionMatcher.Normalize(folder);
            if (host.Model.ExcludedFolders.Any(f => string.Equals(f, normalized, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            host.Model.ExcludedFolders.Add(normalized);
            Persist(host);
            Rescan(host);
        }

        public static void Remove(IngestBenchHost host, string? folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                return;
            }

            string normalized = ScanExclusionMatcher.Normalize(folder);
            for (int i = host.Model.ExcludedFolders.Count - 1; i >= 0; i--)
            {
                if (string.Equals(host.Model.ExcludedFolders[i], normalized, StringComparison.OrdinalIgnoreCase))
                {
                    host.Model.ExcludedFolders.RemoveAt(i);
                }
            }

            if (string.Equals(host.Model.SelectedExclusion, normalized, StringComparison.OrdinalIgnoreCase))
            {
                host.Model.SelectedExclusion = string.Empty;
            }

            Persist(host);
            Rescan(host);
        }

        private static void Persist(IngestBenchHost host) =>
            new ScanExclusionStore(host.Paths).Save(host.Model.ExcludedFolders);

        private static void Rescan(IngestBenchHost host)
        {
            if (host.ScanRoots.Length == 0)
            {
                return;
            }

            host.RefreshScan();
            IngestBenchShoots.Rebuild(host);
        }
    }
}
