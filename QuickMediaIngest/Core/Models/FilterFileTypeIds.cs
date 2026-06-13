#nullable enable
namespace QuickMediaIngest.Core.Models
{
    /// <summary>Culture-invariant toolbar file-type filter IDs (stored in config/view model).</summary>
    public static class FilterFileTypeIds
    {
        public const string AllMedia = "flt:all_media";
        public const string Images = "flt:images";
        public const string Videos = "flt:videos";
        public const string Raw = "flt:raw";
        public const string Jpeg = "flt:jpeg";

        public static string NormalizeStoredValue(string? stored) =>
            stored switch
            {
                null => string.Empty,
                "" => string.Empty,
                "All Media" => AllMedia,
                "Images" => Images,
                "Videos" => Videos,
                "RAW" => Raw,
                "JPG/JPEG" or "JPEG" => Jpeg,
                _ => stored,
            };
    }
}
