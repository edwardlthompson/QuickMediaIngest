using System.Windows;
using Microsoft.Win32;

namespace QuickMediaIngest
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Detect Windows system theme and apply it
            DetectAndApplySystemTheme();
        }
        
        /// <summary>
        /// Detects the Windows system theme (dark/light mode) and applies it to the application.
        /// </summary>
        private void DetectAndApplySystemTheme()
        {
            try
            {
                // Check Windows registry for theme preference
                const string registryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
                const string registryKey = "AppsUseLightTheme";
                
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(registryPath))
                {
                    if (key != null)
                    {
                        object? value = key.GetValue(registryKey);
                        if (value != null && int.TryParse(value.ToString(), out int lightThemeMode))
                        {
                            // 1 = Light Theme, 0 = Dark Theme
                            bool shouldUseLightTheme = lightThemeMode == 1;
                            ApplyTheme(shouldUseLightTheme);
                        }
                        else
                        {
                            // Default to dark theme if key not found
                            ApplyTheme(false);
                        }
                    }
                    else
                    {
                        // Default to dark theme if registry path not found
                        ApplyTheme(false);
                    }
                }
            }
            catch
            {
                // Fail gracefully - default to dark theme
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
                // Find the theme dictionary file to load
                string themeFile = useLightTheme 
                    ? "pack://application:,,,/QuickMediaIngest;component/Themes/LightTheme.xaml"
                    : "pack://application:,,,/QuickMediaIngest;component/Themes/DarkTheme.xaml";
                
                // Load the theme dictionary
                ResourceDictionary themeDict = new ResourceDictionary { Source = new Uri(themeFile) };
                
                // Remove old theme dictionary if it exists
                for (int i = Current.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
                {
                    var dict = Current.Resources.MergedDictionaries[i];
                    if (dict.Source != null && (dict.Source.ToString().Contains("/Themes/DarkTheme.xaml") || 
                        dict.Source.ToString().Contains("/Themes/LightTheme.xaml")))
                    {
                        Current.Resources.MergedDictionaries.RemoveAt(i);
                    }
                }
                
                // Add the new theme dictionary (add at the end for highest priority)
                Current.Resources.MergedDictionaries.Add(themeDict);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
            }
        }
    }
}

