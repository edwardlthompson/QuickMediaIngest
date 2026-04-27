#nullable enable
using System.Collections.Generic;
using System.Linq;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.Services
{
    /// <summary>Orders preview work so expanded shoot groups are serviced first.</summary>
    public static class ThumbnailPreviewOrdering
    {
        public static List<ImportItem> OrderItemsForLocalPreviews(IEnumerable<ItemGroup> groups)
        {
            var ordered = groups
                .OrderByDescending(g => g.IsExpanded)
                .SelectMany(g => g.Items)
                .ToList();
            return ordered;
        }
    }
}
