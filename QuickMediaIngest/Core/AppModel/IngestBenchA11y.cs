#nullable enable
using System;
using QuickMediaIngest.Core.Chrome;
using QuickMediaIngest.Core.Motion;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>High-contrast, reduced motion, RTL, live region for the ingest-bench.</summary>
    public static class IngestBenchA11y
    {
        public const string HighContrastEnv = "QMI_HIGH_CONTRAST";
        public const string ReducedMotionEnv = "QMI_REDUCED_MOTION";

        public static bool DetectHighContrast()
        {
            if (EnvOn(HighContrastEnv))
            {
                return true;
            }

            string? theme = Environment.GetEnvironmentVariable("GTK_THEME");
            return !string.IsNullOrWhiteSpace(theme)
                && theme.Contains("HighContrast", StringComparison.OrdinalIgnoreCase);
        }

        public static bool DetectReducedMotion() =>
            EnvOn(ReducedMotionEnv)
            || EnvOn("PREFERS_REDUCED_MOTION");

        public static void Apply(IngestBenchHost host)
        {
            IngestBenchAppModel model = host.Model;
            model.HighContrast = DetectHighContrast();
            model.ReducedMotion = DetectReducedMotion();
            model.IsRightToLeft = UiReadingOrder.IsRightToLeft(model.Prefs.Language);
            model.OverlayBlurRadius = HighContrastTokens.OverlayBlurRadius(model.HighContrast);
            _ = MotionTimings.Duration(MotionTimings.OverlayEnterMs, model.ReducedMotion);
        }

        public static void Announce(IngestBenchAppModel model, string? text)
        {
            model.AccessibilityAnnouncement = text ?? string.Empty;
        }

        private static bool EnvOn(string name) =>
            string.Equals(Environment.GetEnvironmentVariable(name), "1", StringComparison.Ordinal);
    }
}
