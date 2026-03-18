using System;
using System.Collections.Generic;
using System.Linq;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    public class GroupBuilder
    {
        /// <summary>
        /// Groups items by sorting by date and breaking when a time gap exceeds gapThreshold.
        /// </summary>
        public List<ItemGroup> BuildGroups(List<ImportItem> items, TimeSpan gapThreshold)
        {
            var groups = new List<ItemGroup>();
            if (items == null || items.Count == 0) return groups;

            // Sort by DateTaken to find chronological order
            var sortedItems = items.OrderBy(i => i.DateTaken).ToList();
            
            ItemGroup currentGroup = new ItemGroup
            {
                Title = "Shoot 1",
                StartDate = sortedItems[0].DateTaken,
                EndDate = sortedItems[0].DateTaken
            };
            currentGroup.Items.Add(sortedItems[0]);
            groups.Add(currentGroup);

            for (int i = 1; i < sortedItems.Count; i++)
            {
                var current = sortedItems[i];
                var previous = sortedItems[i - 1];

                if (current.DateTaken - previous.DateTaken > gapThreshold)
                {
                    // Create a new group for next threshold
                    currentGroup = new ItemGroup
                    {
                        Title = $"Shoot {groups.Count + 1}",
                        StartDate = current.DateTaken,
                        EndDate = current.DateTaken
                    };
                    groups.Add(currentGroup);
                }

                currentGroup.Items.Add(current);
                currentGroup.EndDate = current.DateTaken; // Bump end boundary
            }

            return groups;
        }
    }
}
