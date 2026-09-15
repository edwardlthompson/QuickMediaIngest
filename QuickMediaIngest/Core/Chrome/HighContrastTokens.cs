#nullable enable

namespace QuickMediaIngest.Core.Chrome
{
    /// <summary>When Windows high contrast is on, chrome uses system colors and skips overlay blur.</summary>
    public static class HighContrastTokens
    {
        public static bool ShouldApply(bool highContrast) => highContrast;

        public static double OverlayBlurRadius(bool highContrast) => highContrast ? 0d : 8d;
    }
}
