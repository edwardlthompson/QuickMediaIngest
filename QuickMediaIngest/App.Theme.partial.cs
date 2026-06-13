using System;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace QuickMediaIngest
{
    public partial class App
    {
        /// <summary>
        /// Detects the Windows system theme (dark/light mode) and applies it to the application.
        /// </summary>
        private void DetectAndApplySystemTheme()
        {
            try
            {
                const string registryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
                const string registryKey = "AppsUseLightTheme";

                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(registryPath))
                {
                    if (key != null)
                    {
                        object? value = key.GetValue(registryKey);
                        if (value != null && int.TryParse(value.ToString(), out int lightThemeMode))
                        {
                            bool shouldUseLightTheme = lightThemeMode == 1;
                            ApplyTheme(shouldUseLightTheme);
                        }
                        else
                        {
                            ApplyTheme(false);
                        }
                    }
                    else
                    {
                        ApplyTheme(false);
                    }
                }
            }
            catch
            {
                ApplyTheme(false);
            }
        }

        /// <summary>
        /// Applies the specified theme (dark or light) to the application.
        /// </summary>
        public static void ApplyTheme(bool useLightTheme)
        {
            try
            {
                var paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();
                theme.SetBaseTheme(useLightTheme ? BaseTheme.Light : BaseTheme.Dark);

                System.Windows.Media.Color accentColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(useLightTheme ? "#1E88E5" : "#FFEB3B");
                theme.SetPrimaryColor(accentColor);
                theme.SetSecondaryColor(accentColor);

                paletteHelper.SetTheme(theme);
                CurrentIsDarkTheme = !useLightTheme;

                System.Windows.Application.Current.Resources["AppAccentBrush"] = new System.Windows.Media.SolidColorBrush(accentColor);
                System.Windows.Media.SolidColorBrush menuForeground = useLightTheme ? new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1A1A1A")) : new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFFFFF"));
                System.Windows.Media.SolidColorBrush menuBackground = useLightTheme ? new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F5F5F5")) : new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E1E1E"));
                try
                {
                    System.Windows.Application.Current.Resources["MenuForegroundBrush"] = menuForeground;
                    System.Windows.Application.Current.Resources["MenuBackgroundBrush"] = menuBackground;
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "Could not update theme resource brushes.");
                }

                ApplyChromePalette(useLightTheme);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error applying theme.");
            }
        }

        private static void ApplyChromePalette(bool useLightTheme)
        {
            if (Application.Current == null)
            {
                return;
            }

            var res = Application.Current.Resources;

            static void SetBrushColor(ResourceDictionary rd, string key, System.Windows.Media.Color c)
            {
                if (rd[key] is SolidColorBrush existing && !existing.IsFrozen)
                {
                    existing.Color = c;
                }
                else
                {
                    rd[key] = new SolidColorBrush(c);
                }
            }

            static void SetThemeColor(ResourceDictionary rd, string key, System.Windows.Media.Color c)
            {
                rd[key] = c;
            }

            if (useLightTheme)
            {
                SetBrushColor(res, "SidebarBackground", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#E8EAED"));
                SetBrushColor(res, "SidebarVersion", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#5F6368"));
                SetBrushColor(res, "SidebarTitle", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#202124"));
                SetBrushColor(res, "SidebarMenuItem", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#1565C0"));

                SetThemeColor(res, "Theme.Background", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#FFFFFF"));
                SetThemeColor(res, "Theme.Surface", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#FFFFFF"));
                SetThemeColor(res, "Theme.BarBackground", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#F5F5F5"));
                SetThemeColor(res, "Theme.CardBackground", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#FAFAFA"));
                SetThemeColor(res, "Theme.TextPrimary", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#212121"));
                SetThemeColor(res, "Theme.TextSecondary", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#424242"));
                SetThemeColor(res, "Theme.TextTertiary", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#5F6368"));
                SetThemeColor(res, "Theme.Accent", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#0078D4"));
                SetThemeColor(res, "Theme.AccentLight", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#42A5F5"));
                SetThemeColor(res, "Theme.Divider", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#BDBDBD"));
                SetThemeColor(res, "Theme.Hover", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#EEEEEE"));
                SetThemeColor(res, "Theme.Border", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#CCCCCC"));
                SetThemeColor(res, "Theme.Success", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#4CAF50"));
                SetThemeColor(res, "Theme.Warning", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#FFA500"));
                SetThemeColor(res, "Theme.Error", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#F44336"));
                SetThemeColor(res, "Theme.ExcelYellow", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#FFD700"));
            }
            else
            {
                SetBrushColor(res, "SidebarBackground", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#232323"));
                SetBrushColor(res, "SidebarVersion", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#9E9E9E"));
                SetBrushColor(res, "SidebarTitle", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#FFFDEB"));
                SetBrushColor(res, "SidebarMenuItem", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#FFEB3B"));

                SetThemeColor(res, "Theme.Background", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#1E1E1E"));
                SetThemeColor(res, "Theme.Surface", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#2D2D30"));
                SetThemeColor(res, "Theme.BarBackground", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#2D2D30"));
                SetThemeColor(res, "Theme.CardBackground", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#252526"));
                SetThemeColor(res, "Theme.TextPrimary", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#FFFFFF"));
                SetThemeColor(res, "Theme.TextSecondary", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#E0E0E0"));
                SetThemeColor(res, "Theme.TextTertiary", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#B0BEC5"));
                SetThemeColor(res, "Theme.Accent", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#007ACC"));
                SetThemeColor(res, "Theme.AccentLight", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#1084D7"));
                SetThemeColor(res, "Theme.Divider", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#3F3F46"));
                SetThemeColor(res, "Theme.Hover", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#3E3E42"));
                SetThemeColor(res, "Theme.Border", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#3F3F46"));
                SetThemeColor(res, "Theme.Success", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#4CAF50"));
                SetThemeColor(res, "Theme.Warning", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#FFA500"));
                SetThemeColor(res, "Theme.Error", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#F44336"));
                SetThemeColor(res, "Theme.ExcelYellow", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#FFEB3B"));
            }

            SyncThemeSolidBrushesFromColors(res);
        }

        private static void SyncThemeSolidBrushesFromColors(ResourceDictionary rd)
        {
            static void SyncPair(ResourceDictionary dictionary, string colorKey, string brushKey)
            {
                if (dictionary[colorKey] is not System.Windows.Media.Color themeColor)
                {
                    return;
                }

                switch (dictionary[brushKey])
                {
                    case SolidColorBrush existing when !existing.IsFrozen:
                        existing.Color = themeColor;
                        break;
                    default:
                        dictionary[brushKey] = new SolidColorBrush(themeColor);
                        break;
                }
            }

            SyncPair(rd, "Theme.Background", "Theme.Background.Brush");
            SyncPair(rd, "Theme.Surface", "Theme.Surface.Brush");
            SyncPair(rd, "Theme.BarBackground", "Theme.BarBackground.Brush");
            SyncPair(rd, "Theme.CardBackground", "Theme.CardBackground.Brush");
            SyncPair(rd, "Theme.TextPrimary", "Theme.TextPrimary.Brush");
            SyncPair(rd, "Theme.TextSecondary", "Theme.TextSecondary.Brush");
            SyncPair(rd, "Theme.TextTertiary", "Theme.TextTertiary.Brush");
            SyncPair(rd, "Theme.Accent", "Theme.Accent.Brush");
            SyncPair(rd, "Theme.AccentLight", "Theme.AccentLight.Brush");
            SyncPair(rd, "Theme.Divider", "Theme.Divider.Brush");
            SyncPair(rd, "Theme.Hover", "Theme.Hover.Brush");
            SyncPair(rd, "Theme.Border", "Theme.Border.Brush");
            SyncPair(rd, "Theme.Success", "Theme.Success.Brush");
            SyncPair(rd, "Theme.Warning", "Theme.Warning.Brush");
            SyncPair(rd, "Theme.Error", "Theme.Error.Brush");
            SyncPair(rd, "Theme.ExcelYellow", "Theme.ExcelYellow.Brush");
        }
    }
}
