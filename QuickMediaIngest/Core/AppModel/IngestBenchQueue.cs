#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Queue current selection, retry last failures, resume pending-import.json.</summary>
    public static class IngestBenchQueue
    {
        public static void Load(IngestBenchHost host)
        {
            PendingImportPlan? plan = new PendingPlanStore(host.Paths).Load();
            host.Model.HasPendingImportPlan = plan is { SelectedSourcePaths.Count: > 0 };
            Refresh(host);
        }

        public static PendingImportPlan Snapshot(IngestBenchHost host)
        {
            var paths = host.Groups
                .SelectMany(g => g.Items)
                .Where(i => i.IsSelected)
                .Select(i => i.SourcePath)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return new PendingImportPlan
            {
                DestinationRoot = host.Model.DestinationRoot ?? string.Empty,
                NamingTemplate = host.Model.NamingTemplate ?? string.Empty,
                SelectedSourcePaths = paths,
            };
        }

        public static void SavePlan(IngestBenchHost host, PendingImportPlan plan)
        {
            if (plan.SelectedSourcePaths.Count == 0)
            {
                return;
            }

            new PendingPlanStore(host.Paths).Save(plan);
            host.Model.HasPendingImportPlan = true;
            Refresh(host);
        }

        public static void ClearPlan(IngestBenchHost host)
        {
            new PendingPlanStore(host.Paths).Clear();
            host.Model.HasPendingImportPlan = false;
            Refresh(host);
        }

        public static void NoteFailed(IngestBenchHost host, IEnumerable<string> paths)
        {
            foreach (string path in paths)
            {
                if (!string.IsNullOrWhiteSpace(path)
                    && !host.FailedPaths.Contains(path, StringComparer.OrdinalIgnoreCase))
                {
                    host.FailedPaths.Add(path);
                }
            }

            Refresh(host);
        }

        public static void NoteFailedGroup(IngestBenchHost host, ItemGroup group) =>
            NoteFailed(host, group.Items.Where(i => i.IsSelected).Select(i => i.SourcePath));

        public static Task<bool> EnqueueAsync(IngestBenchHost host, IngestBenchCopy copy)
        {
            PendingImportPlan plan = Snapshot(host);
            if (plan.SelectedSourcePaths.Count == 0)
            {
                return Task.FromResult(false);
            }

            if (host.Model.IsImporting)
            {
                host.ImportJobs.Enqueue(plan);
                Refresh(host);
                return Task.FromResult(true);
            }

            ApplySelection(host, plan);
            return host.RunImportAsync(dryRun: false, copy);
        }

        public static Task<bool> ResumeAsync(IngestBenchHost host, IngestBenchCopy copy)
        {
            PendingImportPlan? plan = new PendingPlanStore(host.Paths).Load();
            if (plan is null || plan.SelectedSourcePaths.Count == 0)
            {
                host.Model.HasPendingImportPlan = false;
                Refresh(host);
                return Task.FromResult(false);
            }

            ApplySelection(host, plan);
            return host.RunImportAsync(dryRun: false, copy);
        }

        public static Task<bool> RetryFailedAsync(IngestBenchHost host, IngestBenchCopy copy)
        {
            if (host.FailedPaths.Count == 0)
            {
                return Task.FromResult(false);
            }

            ApplySelection(host, new PendingImportPlan
            {
                DestinationRoot = host.Model.DestinationRoot ?? string.Empty,
                NamingTemplate = host.Model.NamingTemplate ?? string.Empty,
                SelectedSourcePaths = host.FailedPaths.ToList(),
            });
            host.FailedPaths.Clear();
            Refresh(host);
            return host.RunImportAsync(dryRun: false, copy);
        }

        public static Task TryStartNextAsync(IngestBenchHost host, IngestBenchCopy copy)
        {
            if (host.Model.IsImporting || host.ImportJobs.Count == 0)
            {
                Refresh(host);
                return Task.CompletedTask;
            }

            PendingImportPlan next = host.ImportJobs.Dequeue();
            Refresh(host);
            ApplySelection(host, next);
            return IngestBenchImport.RunAsync(host, dryRun: false, copy, resetFailures: false);
        }

        public static void Refresh(IngestBenchHost host)
        {
            host.Model.QueuedImportCount = host.ImportJobs.Count;
            host.Model.FailedImportCount = host.FailedPaths.Count;
        }

        public static void ApplySelection(IngestBenchHost host, PendingImportPlan plan)
        {
            if (!string.IsNullOrWhiteSpace(plan.DestinationRoot))
            {
                host.SetDestination(plan.DestinationRoot);
            }

            if (!string.IsNullOrWhiteSpace(plan.NamingTemplate))
            {
                host.SetNamingTemplate(plan.NamingTemplate);
            }

            var set = new HashSet<string>(plan.SelectedSourcePaths, StringComparer.OrdinalIgnoreCase);
            if (host.Groups.Count == 0 && set.Count > 0)
            {
                string[] roots = set.Select(p => System.IO.Path.GetDirectoryName(p) ?? p).Distinct().ToArray();
                IngestBenchShoots.RebuildFromItems(host, IngestBenchImport.CollectItems(roots, host.Model.ExcludedFolders));
            }

            foreach (ItemGroup group in host.Groups)
            {
                foreach (ImportItem item in group.Items)
                {
                    item.IsSelected = set.Contains(item.SourcePath);
                }
            }

            IngestBenchShoots.ApplyFilter(host);
        }
    }
}
