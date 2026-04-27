#nullable enable
using System;

namespace QuickMediaIngest.Localization
{
    /// <summary>
    /// Stable, culture-invariant IDs for toolbar file-type filter options (stored in view model).
    /// Display strings come from Strings.resx via <see cref="GetDisplayLabel"/>.
    /// </summary>
    public static class FilterFileTypeLocalization
    {
        public const string AllMedia = "flt:all_media";
        public const string Images = "flt:images";
        public const string Videos = "flt:videos";
        public const string Raw = "flt:raw";
        public const string Jpeg = "flt:jpeg";

        /// <summary>Maps legacy English UI values from older builds into stable IDs.</summary>
        public static string NormalizeStoredValue(string? stored)
        {
            return stored switch
            {
                null => string.Empty,
                "" => "",
                "All Media" => AllMedia,
                "Images" => Images,
                "Videos" => Videos,
                "RAW" => Raw,
                "JPG/JPEG" or "JPEG" => Jpeg,
                _ => stored,
            };
        }

        /// <summary>Localized label for combo box rows and filter chips.</summary>
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
                _ => stored ?? "",
            };
        }
    }
}
