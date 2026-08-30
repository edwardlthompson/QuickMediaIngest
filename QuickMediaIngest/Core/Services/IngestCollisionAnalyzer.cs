#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.Services
{
    public sealed class CollisionReportItem
    {
        public string SourcePath { get; set; } = string.Empty;
        public string PlannedDestinationPath { get; set; } = string.Empty;
        public bool ExistsAtDestination { get; set; }
        public string ActionTaken { get; set; } = string.Empty;
    }

    public sealed class CollisionReport
    {
        public int TotalFiles { get; set; }
        public int CollisionsCount { get; set; }
        public List<CollisionReportItem> Items { get; set; } = new();
    }

    public static class IngestCollisionAnalyzer
    {
        public static CollisionReport Analyze(
            IEnumerable<ItemGroup> groups,
            string destinationRoot,
            string namingTemplate,
            DuplicateHandlingMode mode)
        {
            var report = new CollisionReport();
            var itemsList = new List<CollisionReportItem>();

            foreach (var group in groups)
            {
                string folderName = GroupFolderNaming.GetTargetFolderName(group);
                string targetDir = Path.Combine(destinationRoot, folderName);
                int seq = 0;

                foreach (var item in group.Items.Where(i => i.IsSelected))
                {
                    seq++;
                    report.TotalFiles++;
                    string resolvedName = IngestFileNaming.ResolveFileName(
                        item,
                        targetDir,
                        namingTemplate,
                        group.Title,
                        seq,
                        mode,
                        out bool skipped);

                    string plannedPath = Path.Combine(targetDir, resolvedName);
                    string defaultPath = Path.Combine(targetDir, IngestFileNaming.BuildBaseFileName(item, namingTemplate, group.Title, seq));
                    bool exists = File.Exists(defaultPath);

                    string action = mode switch
                    {
                        DuplicateHandlingMode.Skip when skipped => "Skip",
                        DuplicateHandlingMode.Suffix when exists => "Suffix",
                        DuplicateHandlingMode.OverwriteIfNewer when exists => "Overwrite",
                        _ => "Copy"
                    };

                    if (exists)
                    {
                        report.CollisionsCount++;
                    }

                    itemsList.Add(new CollisionReportItem
                    {
                        SourcePath = item.SourcePath,
                        PlannedDestinationPath = plannedPath,
                        ExistsAtDestination = exists,
                        ActionTaken = action
                    });
                }
            }

            report.Items = itemsList;
            return report;
        }
    }
}
