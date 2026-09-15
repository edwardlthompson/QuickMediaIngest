#nullable enable
using System;
using Microsoft.Win32;

namespace QuickMediaIngest.Core.Services
{
    public enum AppThemeMode
    {
        System = 0,
        Light = 1,
        Dark = 2
    }

    public static class SystemThemeDetector
    {
        public static bool IsWindowsDarkThemePreferred()
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            try
            {
                const string registryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
                const string registryKey = "AppsUseLightTheme";

                using var key = Registry.CurrentUser.OpenSubKey(registryPath);
                if (key != null)
                {
                    object? value = key.GetValue(registryKey);
                    if (value is int intVal)
                    {
                        return intVal == 0;
                    }
                }
            }
            catch
            {
                // Fall back to default
            }

            return false;
        }

        public static bool ResolveIsDark(AppThemeMode mode)
        {
            return mode switch
            {
                AppThemeMode.Dark => true,
                AppThemeMode.Light => false,
                _ => IsWindowsDarkThemePreferred()
            };
        }
    }
}
