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
                try
                {
                        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        string baseDir = Path.Combine(appData, "QuickMediaIngest");
                        string logsDir = Path.Combine(baseDir, "Logs");
                        Directory.CreateDirectory(logsDir);
                        string fatalPath = Path.Combine(baseDir, "fatal.log");
                        File.AppendAllText(fatalPath, $"[UI] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {e.Exception}\n");
                        // Also write a timestamped crash file for easier collection
                        string crashFile = Path.Combine(logsDir, $"crash-ui-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
                        File.WriteAllText(crashFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Unhandled UI exception:\n{e.Exception}\n");
                }
                catch { }
                MessageBox.Show($"A fatal error occurred:\n{e.Exception}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            }

            private void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
            {
                try
                {
                        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        string baseDir = Path.Combine(appData, "QuickMediaIngest");
                        string logsDir = Path.Combine(baseDir, "Logs");
                        Directory.CreateDirectory(logsDir);
                        string fatalPath = Path.Combine(baseDir, "fatal.log");
                        File.AppendAllText(fatalPath, $"[Domain] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {e.ExceptionObject}\n");
                        string crashFile = Path.Combine(logsDir, $"crash-domain-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
                        File.WriteAllText(crashFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Unhandled domain exception:\n{e.ExceptionObject}\n");
                }
                catch { }
                MessageBox.Show($"A fatal error occurred:\n{e.ExceptionObject}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

            // Detect Windows system theme and apply it
            DetectAndApplySystemTheme();

            // Allow forcing a controlled test crash by creating a file named 'qmi_force_crash.txt' in the temp folder.
            // Useful for verifying the unhandled-exception logging behavior during manual tests.
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
                // Complete startup work while splash is visible.
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
                Color accentColor = (Color)ColorConverter.ConvertFromString(useLightTheme ? "#1E88E5" : "#FFEB3B");
                theme.SetPrimaryColor(accentColor);
                theme.SetSecondaryColor(accentColor);

                paletteHelper.SetTheme(theme);
                CurrentIsDarkTheme = !useLightTheme;

                Current.Resources["AppAccentBrush"] = new SolidColorBrush(accentColor);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error applying theme.");
            }
        }
    }
}

