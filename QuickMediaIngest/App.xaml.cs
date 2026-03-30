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
            _serviceProvider = ConfigureServices();
            _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
            _logger.LogInformation("Application startup initiated.");

            // Health check: detect previous crash
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string baseDir = Path.Combine(appData, "QuickMediaIngest");
            string runMarker = Path.Combine(baseDir, "last_run.tmp");
            string logsDir = Path.Combine(baseDir, "logs");
            bool crashed = File.Exists(runMarker);
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

            // If previous run crashed, prompt user
            if (crashed)
            {
                string logMsg = "It looks like the app closed unexpectedly last time. Would you like to view the error log?";
                if (MessageBox.Show(logMsg, "Crash Detected", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        string logPath = logsDir;
                        if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);
                        Process.Start(new ProcessStartInfo("explorer.exe", logPath) { UseShellExecute = true });
                    }
                    catch { }
                }
            }
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
                System.Windows.Media.SolidColorBrush menuBackground = useLightTheme ? new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFFFFF")) : new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E1E1E"));
                try
                {
                    System.Windows.Application.Current.Resources["MenuForegroundBrush"] = menuForeground;
                    System.Windows.Application.Current.Resources["MenuBackgroundBrush"] = menuBackground;
                }
                catch { }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error applying theme.");
            }
        }
    }
}

