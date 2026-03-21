#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Provides logic to group import items into chronological groups based on time gaps.
    /// </summary>
    public class GroupBuilder
    {
        /// <summary>
        /// Returns a folder name for the group, suitable for use as a directory name.
        /// </summary>
        /// <param name="group">The item group.</param>
        /// <returns>A folder name string.</returns>
        public string GetTargetFolderName(Models.ItemGroup group)
        {
            // Use group title and start date for uniqueness
            string datePart = group.StartDate.ToString("yyyyMMdd_HHmmss");
            string safeTitle = string.Join("_", group.Title.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
            if (string.IsNullOrWhiteSpace(safeTitle)) safeTitle = "Group";
            return $"{datePart}_{safeTitle}";
        }

        /// <summary>
        /// Groups items by sorting by date and breaking into new groups when a time gap exceeds the specified threshold.
        /// </summary>
        /// <param name="items">List of import items to group.</param>
        /// <param name="gapThreshold">Time gap threshold for starting a new group.</param>
        /// <returns>List of grouped items.</returns>
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
