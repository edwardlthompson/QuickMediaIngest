#nullable enable
using System;
using System.Windows;

namespace QuickMediaIngest.Core.Services
{
    public static class AccessibilityPreferencesDetector
    {
        public static bool IsHighContrastActive()
        {
            if (OperatingSystem.IsWindows())
            {
                return SystemParameters.HighContrast;
            }
            return false;
        }

        public static bool IsReducedMotionPreferred()
        {
            // In Windows 10/11, animations can be turned off in Ease of Access / Accessibility settings
            if (OperatingSystem.IsWindows())
            {
                return !SystemParameters.ClientAreaAnimation;
            }
            return false;
        }
    }
}
