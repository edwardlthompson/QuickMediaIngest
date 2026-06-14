#nullable enable

namespace QuickMediaIngest.Core
{
    internal enum FtpPreviewDecodeMode
    {
        /// <summary>Early tiers — embedded preview only; never libvips or full decode chain.</summary>
        TieredPartial = 0,

        /// <summary>Final type cap tier — allow Magick on capped buffer.</summary>
        TieredFinalCap = 1,

        /// <summary>Complete file on disk — full decode chain.</summary>
        CompleteFile = 2
    }
}
