#nullable enable
using System.Collections.Generic;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.ViewModels;

namespace QuickMediaIngest.Core.Services
{
    public interface IShootFilterService
    {
        /// <summary>Date, file type, and filename keyword rules (used by <see cref="System.Windows.Data.CollectionViewSource"/> and shoot cards).</summary>
        bool PassesToolbarRules(ImportItem item, ShootFilterCriteria criteria);

        /// <summary>AND-combines <see cref="PassesToolbarRules"/> with existing <see cref="ImportItem.IsPreviewVisible"/> (after stack layout).</summary>
        void ApplyToolbarFilters(IReadOnlyList<ItemGroup> groups, ShootFilterCriteria criteria);
    }
}
