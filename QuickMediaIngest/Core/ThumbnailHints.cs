#nullable enable
namespace QuickMediaIngest.Core
{
    /// <summary>Optional hints for thumbnail generation (preview prioritization and RAW shell deferral).</summary>
    public sealed class ThumbnailHints
    {
        /// <summary>Milliseconds to yield before expensive RAW shell extraction (smoother scrolling).</summary>
        public int DeferRawShellMilliseconds { get; init; }

        /// <summary>True when the on-disk bytes are a capped FTP/ADB preview buffer (not a complete file).</summary>
        public bool IsPartialPreview { get; init; }

        public static ThumbnailHints Default => new();

        public static ThumbnailHints PartialPreview => new() { IsPartialPreview = true };
    }
}
