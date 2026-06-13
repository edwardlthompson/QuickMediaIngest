#nullable enable
using System;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Localization
{
    /// <summary>
    /// Stable, culture-invariant IDs for toolbar file-type filter options (stored in view model).
    /// Display strings come from Strings.resx via <see cref="GetDisplayLabel"/>.
    /// </summary>
    public static class FilterFileTypeLocalization
    {
        public const string AllMedia = FilterFileTypeIds.AllMedia;
        public const string Images = FilterFileTypeIds.Images;
        public const string Videos = FilterFileTypeIds.Videos;
        public const string Raw = FilterFileTypeIds.Raw;
        public const string Jpeg = FilterFileTypeIds.Jpeg;

        public static string NormalizeStoredValue(string? stored) => FilterFileTypeIds.NormalizeStoredValue(stored);

        public static string GetDisplayLabel(string? stored)
        {
            stored = NormalizeStoredValue(stored);
            if (string.IsNullOrEmpty(stored))
            {
                return AppLocalizer.Get("Filter_FileType_NoFilter");
            }

            return stored switch
            {
                AllMedia => AppLocalizer.Get("Filter_FileType_AllMedia"),
                Images => AppLocalizer.Get("Filter_FileType_Images"),
                Videos => AppLocalizer.Get("Filter_FileType_Videos"),
                Raw => AppLocalizer.Get("Filter_FileType_RAW"),
                Jpeg => AppLocalizer.Get("Filter_FileType_JPEG"),
                _ => stored ?? string.Empty,
            };
        }
    }
}
