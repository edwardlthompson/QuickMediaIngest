#nullable enable
using System;
using System.Collections.Generic;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Localization;
using QuickMediaIngest.ViewModels;

namespace QuickMediaIngest.Core.Services
{
    public sealed class ShootFilterService : IShootFilterService
    {
        private static readonly HashSet<string> RawPreviewExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".dng", ".cr2", ".cr3", ".nef", ".arw", ".raf", ".orf", ".rw2", ".srw"
        };

        public bool PassesToolbarRules(ImportItem item, ShootFilterCriteria criteria)
        {
            if (criteria.FilterStartDate.HasValue && item.DateTaken < criteria.FilterStartDate.Value.Date)
            {
                return false;
            }

            if (criteria.FilterEndDate.HasValue &&
                item.DateTaken > criteria.FilterEndDate.Value.Date.AddDays(1).AddTicks(-1))
            {
                return false;
            }

            if (!MatchesFileTypeFilter(item, criteria.FilterFileType))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(criteria.FilterKeyword) &&
                !item.FileName.Contains(criteria.FilterKeyword, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        public void ApplyToolbarFilters(IReadOnlyList<ItemGroup> groups, ShootFilterCriteria criteria)
        {
            foreach (var group in groups)
            {
                foreach (var item in group.Items)
                {
                    item.IsPreviewVisible = item.IsPreviewVisible && PassesToolbarRules(item, criteria);
                }
            }
        }

        private static bool MatchesFileTypeFilter(ImportItem item, string selectedFilter)
        {
            selectedFilter = FilterFileTypeLocalization.NormalizeStoredValue(selectedFilter);
            if (string.IsNullOrWhiteSpace(selectedFilter))
            {
                return true;
            }

            string extension = item.FileType?.TrimStart('.').ToUpperInvariant() ?? string.Empty;
            switch (selectedFilter)
            {
                case FilterFileTypeLocalization.AllMedia:
                    return true;
                case FilterFileTypeLocalization.Images:
                    return !item.IsVideo;
                case FilterFileTypeLocalization.Videos:
                    return item.IsVideo;
                case FilterFileTypeLocalization.Raw:
                    return RawPreviewExtensions.Contains($".{extension.ToLowerInvariant()}");
                case FilterFileTypeLocalization.Jpeg:
                    return extension is "JPG" or "JPEG";
                default:
                    return string.Equals(item.FileType, selectedFilter, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
