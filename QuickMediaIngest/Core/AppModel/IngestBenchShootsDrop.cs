#nullable enable
using System;
using System.Linq;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Remove one imported file from the shoot grid (UI thread only).</summary>
    internal static class IngestBenchShootsDrop
    {
        public static bool DropOne(IngestBenchHost host, string sourcePath, bool refresh)
        {
            ItemGroup? group = null;
            ImportItem? found = null;
            foreach (ItemGroup candidate in host.Groups.ToArray())
            {
                foreach (ImportItem item in candidate.Items.ToArray())
                {
                    if (string.Equals(item.SourcePath, sourcePath, StringComparison.OrdinalIgnoreCase))
                    {
                        group = candidate;
                        found = item;
                        break;
                    }
                }

                if (found != null)
                {
                    break;
                }
            }

            if (group is null || found is null)
            {
                return false;
            }

            int idx = group.Items.IndexOf(found);
            group.Items.Remove(found);
            group.NotifyItemsChanged();
            IngestBenchThumbs.ForgetImported(host, new[] { found });
            if (ReferenceEquals(host.Model.SelectedCullItem, found))
            {
                host.Model.SelectedCullItem = idx >= 0 && idx < group.Items.Count
                    ? group.Items[idx]
                    : group.Items.LastOrDefault();
            }

            bool gone = group.Items.Count == 0;
            if (gone)
            {
                host.Groups.Remove(group);
            }

            if (refresh || gone)
            {
                IngestBenchShoots.ApplyFilter(host);
                if (gone && host.Model.SelectedShoot == group)
                {
                    host.Model.SelectedShoot = host.Model.Shoots.FirstOrDefault();
                }
            }

            host.Model.NoteSources(host.Groups.Sum(g => g.Items.Count));
            return gone;
        }
    }
}
