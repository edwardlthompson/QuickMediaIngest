#nullable enable
using System;

namespace QuickMediaIngest.Core.Motion
{
    /// <summary>Ingest-bench motion tokens. All durations are 0 when reduced motion is on.</summary>
    public static class MotionTimings
    {
        public const int OverlayEnterMs = 180;
        public const int ChevronMs = 120;
        public const int ImportPressMs = 90;
        public const int RowFadeMs = 160;
        public const int SkeletonPulseMs = 900;
        public const int SuccessFlashMs = 220;
        public const int ChipCollapseMs = 140;
        public const int SidebarMs = 200;
        public const double SidebarExpandedPx = 260;
        public const double SidebarCollapsedPx = 64;
        public const double ImportPressScale = 0.96;

        public static TimeSpan Duration(int milliseconds, bool reducedMotion)
        {
            if (reducedMotion || milliseconds <= 0)
            {
                return TimeSpan.Zero;
            }

            return TimeSpan.FromMilliseconds(milliseconds);
        }
    }
}
