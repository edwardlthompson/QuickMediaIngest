#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.Services
{
    public static class ShootTitleBatchRenamer
    {
        public static void RenameShootsWithUniqueness(IEnumerable<ItemGroup> groups, string baseTitle)
        {
            if (groups == null) return;
            string cleanBase = string.IsNullOrWhiteSpace(baseTitle) ? "Shoot" : baseTitle.Trim();

            var list = groups.ToList();
            if (list.Count == 0) return;

            if (list.Count == 1)
            {
                list[0].Title = cleanBase;
                return;
            }

            var usedTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < list.Count; i++)
            {
                string candidate = $"{cleanBase} {i + 1}";
                int suffix = 1;
                while (usedTitles.Contains(candidate))
                {
                    suffix++;
                    candidate = $"{cleanBase} {i + 1}_{suffix}";
                }
                usedTitles.Add(candidate);
                list[i].Title = candidate;
            }
        }
    }
}
