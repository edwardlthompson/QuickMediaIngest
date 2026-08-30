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
        public string GetTargetFolderName(Models.ItemGroup group) =>
            GroupFolderNaming.GetTargetFolderName(group);

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

        /// <summary>
        /// Splits a group into two groups at the specified item index.
        /// </summary>
        public static (ItemGroup primary, ItemGroup secondary) SplitGroup(ItemGroup group, int splitIndex, string secondaryTitle)
        {
            if (group == null || group.Items.Count <= 1 || splitIndex <= 0 || splitIndex >= group.Items.Count)
            {
                throw new ArgumentException("Invalid split index or group size.");
            }

            var secondaryItems = group.Items.Skip(splitIndex).ToList();
            group.Items = group.Items.Take(splitIndex).ToList();
            group.StartDate = group.Items.Min(i => i.DateTaken);
            group.EndDate = group.Items.Max(i => i.DateTaken);

            var secondaryGroup = new ItemGroup
            {
                Title = secondaryTitle,
                StartDate = secondaryItems.Min(i => i.DateTaken),
                EndDate = secondaryItems.Max(i => i.DateTaken),
                Items = secondaryItems
            };

            return (group, secondaryGroup);
        }

        /// <summary>
        /// Merges two item groups into a single combined item group.
        /// </summary>
        public static ItemGroup MergeGroups(ItemGroup targetGroup, ItemGroup sourceGroup)
        {
            if (targetGroup == null || sourceGroup == null)
            {
                throw new ArgumentNullException(nameof(targetGroup));
            }

            targetGroup.Items.AddRange(sourceGroup.Items);
            targetGroup.Items = targetGroup.Items.OrderBy(i => i.DateTaken).ToList();
            targetGroup.StartDate = targetGroup.Items.Min(i => i.DateTaken);
            targetGroup.EndDate = targetGroup.Items.Max(i => i.DateTaken);

            return targetGroup;
        }
    }
}
