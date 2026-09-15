#nullable enable

namespace QuickMediaIngest.Core.Chrome
{
    /// <summary>When the window is under this width, View collapses to “…” and the destination chip docks into overflow.</summary>
    public static class CommandBarOverflow
    {
        public const double CompactWidthPx = 1100;

        public static bool IsCompact(double width) => width > 0 && width < CompactWidthPx;
    }
}
