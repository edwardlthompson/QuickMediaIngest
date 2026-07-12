using System.Diagnostics;
using System;
using QuickMediaIngest.Localization;
using System.Net.Http;
using System.IO;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using NetVips;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Logging;
using QuickMediaIngest.Core.Services;
using QuickMediaIngest.Data;
using QuickMediaIngest.Services;
using QuickMediaIngest.Thumbnails.Wpf;
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
                catch (Exception logEx)
                {
                    Trace.TraceWarning("Failed to persist UI crash artifact: {0}", logEx);
                }
            });
            System.Windows.MessageBox.Show(AppLocalizer.Get("Msg_Unhandled_Error_Body"), AppLocalizer.Get("Msg_Unhandled_Error_Title"), MessageBoxButton.OK, MessageBoxImage.Error);
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
                catch (Exception logEx)
                {
                    Trace.TraceWarning("Failed to persist domain crash artifact: {0}", logEx);
                }
            });
            System.Windows.MessageBox.Show(AppLocalizer.Get("Msg_Unhandled_Error_Body"), AppLocalizer.Get("Msg_Unhandled_Error_Title"), MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static string TryGetAppConfigDump()
        {
            try
            {
                string configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "QuickMediaIngest",
                    "config.json");
                if (!File.Exists(configPath))
                {
                    return "(No config file found)";
                }

                string json = File.ReadAllText(configPath);
                var config = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(json);
                if (config == null)
                {
                    return "(Failed to parse config.json)";
                }

                // Never write FTP secrets into crash artifacts.
                config.FtpPass = string.Empty;
                return System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            }
            catch { return "(Failed to read AppConfig)"; }
        }
        public static bool CurrentIsDarkTheme { get; private set; } = true;
        private ServiceProvider? _serviceProvider;
        private static ILogger<App>? _logger;

        protected override async void OnStartup(StartupEventArgs e)
        {
            if (TryRunHeadlessSmoke(e.Args, out int smokeExitCode))
            {
                Shutdown(smokeExitCode);
                return;
            }

            base.OnStartup(e);
            LocalizationService.ApplyCultureFromConfigFileEarly();
            _serviceProvider = ConfigureServices();
            _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
            _logger.LogInformation("Application startup initiated.");

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string baseDir = Path.Combine(appData, "QuickMediaIngest");
            string runMarker = Path.Combine(baseDir, "last_run.tmp");
            try
            {
                Directory.CreateDirectory(baseDir);
                File.WriteAllText(runMarker, DateTime.Now.ToString("O"));
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Could not write run marker: {0}", ex);
            }

            DetectAndApplySystemTheme();

            string crashMarker = Path.Combine(Path.GetTempPath(), "qmi_force_crash.txt");
            if (File.Exists(crashMarker))
            {
                throw new Exception("Forced test crash for verification (qmi_force_crash.txt present)");
            }

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
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string baseDir = Path.Combine(appData, "QuickMediaIngest");
                string runMarker = Path.Combine(baseDir, "last_run.tmp");
                if (File.Exists(runMarker)) File.Delete(runMarker);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Could not remove run marker: {0}", ex);
            }
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
            services.AddSingleton<IUpdateService, UpdateService>();
            services.AddSingleton<IDatabaseService, DatabaseService>();
            services.AddSingleton<IDeviceWatcher, DeviceWatcher>();
            services.AddSingleton<IFileProviderFactory, FileProviderFactory>();
            services.AddSingleton<IIngestEngineFactory, IngestEngineFactory>();
            services.AddSingleton<GroupBuilder>();
            services.AddSingleton<IShootFilterService, ShootFilterService>();
            services.AddSingleton<IFtpWorkflowService, FtpWorkflowService>();
            services.AddSingleton<IUnifiedConcreteSourceScanService, UnifiedConcreteSourceScanService>();
            services.AddSingleton<FtpFileDownloader>();
            services.AddSingleton<IFtpThumbnailService, FtpThumbnailService>();
            services.AddSingleton<IFtpCredentialStore, WindowsFtpCredentialStore>();
            services.AddSingleton<IFileDialogService, WpfFileDialogService>();
            services.AddSingleton<IShellService, WpfShellService>();

            services.AddSingleton(typeof(ILogger<AdbFileProvider>), sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger<AdbFileProvider>());

            services.AddTransient<MainViewModel>();
            services.AddTransient<MainWindow>();

            return services.BuildServiceProvider();
        }

        private static bool TryRunHeadlessSmoke(string[] args, out int exitCode)
        {
            exitCode = 0;
            foreach (string arg in args)
            {
                if (!string.Equals(arg, "--smoke-libvips", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    _ = NetVips.NetVips.Version(0);
                    Console.WriteLine("OK libvips");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"FAIL libvips: {ex.Message}");
                    exitCode = 1;
                    return true;
                }
            }

            return false;
        }
    }
}
