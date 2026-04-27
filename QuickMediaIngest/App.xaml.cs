using System.Diagnostics;
using System;
using System.Net.Http;
using System.IO;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Logging;
using QuickMediaIngest.Data;
using QuickMediaIngest.Localization;
using QuickMediaIngest.ViewModels;

namespace QuickMediaIngest
{
    public partial class App : System.Windows.Application
    {

        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }


        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string baseDir = Path.Combine(appData, "QuickMediaIngest");
                    string logsDir = Path.Combine(baseDir, "logs");
                    Directory.CreateDirectory(logsDir);
                    string fatalPath = Path.Combine(baseDir, "fatal.log");
                    File.AppendAllText(fatalPath, $"[UI] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {e.Exception}\n");
                    string crashFile = Path.Combine(logsDir, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                    string configDump = TryGetAppConfigDump();
                    File.WriteAllText(crashFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Unhandled UI exception:\n{e.Exception}\n\nAppConfig:\n{configDump}\n");
                }
                catch { }
            });
            System.Windows.MessageBox.Show($"An unexpected error occurred and was logged. A crash log has been saved to your logs folder.", "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }


        private void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string baseDir = Path.Combine(appData, "QuickMediaIngest");
                    string logsDir = Path.Combine(baseDir, "logs");
                    Directory.CreateDirectory(logsDir);
                    string fatalPath = Path.Combine(baseDir, "fatal.log");
                    File.AppendAllText(fatalPath, $"[Domain] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {e.ExceptionObject}\n");
                    string crashFile = Path.Combine(logsDir, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                    string configDump = TryGetAppConfigDump();
                    File.WriteAllText(crashFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Unhandled domain exception:\n{e.ExceptionObject}\n\nAppConfig:\n{configDump}\n");
                }
                catch { }
            });
            System.Windows.MessageBox.Show($"An unexpected error occurred and was logged. A crash log has been saved to your logs folder.", "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static string TryGetAppConfigDump()
        {
            try
            {
                // Try to get the current AppConfig state from the ViewModel or static config
                // (This is a placeholder; replace with actual config serialization if available)
                return File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest", "appconfig.json"))
                    ? File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest", "appconfig.json"))
                    : "(No config file found)";
            }
            catch { return "(Failed to read AppConfig)"; }
        }
        public static bool CurrentIsDarkTheme { get; private set; } = true;
        private ServiceProvider? _serviceProvider;
        private static ILogger<App>? _logger;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            LocalizationService.ApplyCultureFromConfigFileEarly();
            _serviceProvider = ConfigureServices();
            _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
            _logger.LogInformation("Application startup initiated.");

            // Run marker for clean shutdown (marker removed on normal exit)
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string baseDir = Path.Combine(appData, "QuickMediaIngest");
            string runMarker = Path.Combine(baseDir, "last_run.tmp");
            // Mark this run as started
            try { Directory.CreateDirectory(baseDir); File.WriteAllText(runMarker, DateTime.Now.ToString("O")); } catch { }

            // Detect Windows system theme and apply it
            DetectAndApplySystemTheme();

            // Allow forcing a controlled test crash by creating a file named 'qmi_force_crash.txt' in the temp folder.
            try
            {
                string crashMarker = Path.Combine(Path.GetTempPath(), "qmi_force_crash.txt");
                if (File.Exists(crashMarker))
                {
                    throw new Exception("Forced test crash for verification (qmi_force_crash.txt present)");
                }
            }
            catch { }

            var splash = new SplashWindow();
            splash.Show();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Hide();

            if (mainWindow.DataContext is MainViewModel vm)
            {
                await vm.InitializeAsync();
                mainWindow.ApplyWindowStateFromViewModel();
            }

            MainWindow = mainWindow;
            mainWindow.Show();
            splash.Close();
            _logger.LogInformation("Application startup completed.");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Remove run marker to indicate clean exit
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string baseDir = Path.Combine(appData, "QuickMediaIngest");
                string runMarker = Path.Combine(baseDir, "last_run.tmp");
                if (File.Exists(runMarker)) File.Delete(runMarker);
            }
            catch { }
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }

        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            string logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest");
            string logPath = Path.Combine(logFolder, "quickmediaingest.log");

            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddProvider(new FileLoggerProvider(logPath, LogLevel.Information));
            });

            services.AddSingleton<HttpClient>(_ =>
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("QuickMediaIngest-Updater");
                return client;
            });


            services.AddSingleton<ILocalScanner, LocalScanner>();
            services.AddSingleton<IFtpScanner, FtpScanner>();
            services.AddSingleton<IThumbnailService, ThumbnailService>();
            services.AddSingleton<IMetadataReader, MetadataReader>();
            services.AddSingleton<IWhitelistFilter, WhitelistFilter>();
            services.AddSingleton<IUpdateService, UpdateService>();
            services.AddSingleton<IDatabaseService, DatabaseService>();
            services.AddSingleton<IDeviceWatcher, DeviceWatcher>();
            services.AddSingleton<IFileProviderFactory, FileProviderFactory>();
            services.AddSingleton<IIngestEngineFactory, IngestEngineFactory>();
            services.AddSingleton<GroupBuilder>();

            // Register loggers for all file providers
            services.AddSingleton(typeof(ILogger<AdbFileProvider>), sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger<AdbFileProvider>());

            services.AddTransient<MainViewModel>();
            services.AddTransient<MainWindow>();

            return services.BuildServiceProvider();
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
                var paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();
                theme.SetBaseTheme(useLightTheme ? Theme.Light : Theme.Dark);

                // Dark mode uses classic yellow accents, light mode uses blue accents.
                System.Windows.Media.Color accentColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(useLightTheme ? "#1E88E5" : "#FFEB3B");
                theme.SetPrimaryColor(accentColor);
                theme.SetSecondaryColor(accentColor);

                paletteHelper.SetTheme(theme);
                CurrentIsDarkTheme = !useLightTheme;

                System.Windows.Application.Current.Resources["AppAccentBrush"] = new System.Windows.Media.SolidColorBrush(accentColor);
                // Update menu brushes to ensure visibility when switching themes
                System.Windows.Media.SolidColorBrush menuForeground = useLightTheme ? new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1A1A1A")) : new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFFFFF"));
                // Light: soft gray toolbar band under white paper; dark: charcoal strip
                System.Windows.Media.SolidColorBrush menuBackground = useLightTheme ? new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F5F5F5")) : new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E1E1E"));
                try
                {
                    System.Windows.Application.Current.Resources["MenuForegroundBrush"] = menuForeground;
                    System.Windows.Application.Current.Resources["MenuBackgroundBrush"] = menuBackground;
                }
                catch { }

                ApplyChromePalette(useLightTheme);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error applying theme.");
            }
        }

        /// <summary>
        /// Updates sidebar and Theme.* palette so chrome inverts with light/dark mode
        /// (MaterialDesign paper alone does not drive SidebarBackground or custom Theme keys).
        /// </summary>
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
                // Sidebar: slightly darker than main paper so layout reads clearly
                SetBrushColor(res, "SidebarBackground", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#E8EAED"));
                SetBrushColor(res, "SidebarVersion", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#5F6368"));
                SetBrushColor(res, "SidebarTitle", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#202124"));
                SetBrushColor(res, "SidebarMenuItem", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#1565C0"));

                SetThemeColor(res, "Theme.Background", (System.Windows.Media.Color)ColorConverter.ConvertFromString("#FFFFFF"));
                // White control faces (buttons, rows) on gray toolbar / sidebar — was same as bar and read as solid "black" MD blocks
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

            // SolidColorBrush resources in Themes/Brushes.xaml bind Color from Theme.* — replacing the Color
            // resource does not always invalidate those brushes under mixed merged dictionaries.
            SyncThemeSolidBrushesFromColors(res);
        }

        /// <summary>
        /// Copies each Theme.* Color into matching Theme.*.Brush so chrome (sidebar rows, toolbar buttons)
        /// picks up light/dark palette updates immediately.
        /// </summary>
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

