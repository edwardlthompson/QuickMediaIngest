#nullable enable
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using QuickMediaIngest;
using QuickMediaIngest.ViewModels;
using Xunit;

namespace QuickMediaIngest.Tests
{
    [Collection("Wpf")]
    public class MainViewModelConfigReloadTests
    {
        private static string ConfigPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest", "config.json");

        [Fact]
        public void LoadConfig_RestoresDeleteAfterImportAndThumbnailSize()
        {
            WpfTestHost.EnsureInitialized();
            string? backup = BackupConfigIfPresent();

            try
            {
                string folder = Path.GetDirectoryName(ConfigPath)!;
                Directory.CreateDirectory(folder);

                var saved = new AppConfig
                {
                    DeleteAfterImport = true,
                    DeleteAfterImportPromptDismissed = true,
                    ThumbnailSize = 200,
                    ThumbnailPerformanceMode = "Ultra"
                };
                File.WriteAllText(ConfigPath, JsonSerializer.Serialize(saved));

                MainViewModel vm = MainViewModelProofTests.CreateViewModel();
                InvokeLoadConfig(vm);

                Assert.True(vm.DeleteAfterImport);
                Assert.True(vm.DeleteAfterImportPromptDismissed);
                Assert.Equal(200, vm.ThumbnailSize);
                Assert.Equal("Ultra", vm.ThumbnailPerformanceMode);
            }
            finally
            {
                RestoreConfig(backup);
            }
        }

        [Fact]
        public void SaveConfig_ThenLoadConfig_RoundTripsPersistedFields()
        {
            WpfTestHost.EnsureInitialized();
            string? backup = BackupConfigIfPresent();

            try
            {
                MainViewModel vm = MainViewModelProofTests.CreateViewModel();
                vm.DeleteAfterImport = true;
                vm.DeleteAfterImportPromptDismissed = true;
                vm.ThumbnailSize = 200;
                vm.ThumbnailPerformanceMode = "Ultra";
                vm.SaveConfig();

                MainViewModel reloaded = MainViewModelProofTests.CreateViewModel();
                InvokeLoadConfig(reloaded);

                Assert.True(reloaded.DeleteAfterImport);
                Assert.True(reloaded.DeleteAfterImportPromptDismissed);
                Assert.Equal(200, reloaded.ThumbnailSize);
                Assert.Equal("Ultra", reloaded.ThumbnailPerformanceMode);
            }
            finally
            {
                RestoreConfig(backup);
            }
        }

        [Fact]
        public void LoadConfig_MigratesLegacyFtpPass_AndPurgesPlaintextFromDisk()
        {
            WpfTestHost.EnsureInitialized();
            string? backup = BackupConfigIfPresent();

            try
            {
                string folder = Path.GetDirectoryName(ConfigPath)!;
                Directory.CreateDirectory(folder);

                const string legacySecret = "legacy-ftp-secret-do-not-keep";
                var saved = new AppConfig
                {
                    FtpHost = "10.0.0.23",
                    FtpPort = 2221,
                    FtpUser = "camera",
                    FtpPass = legacySecret,
                    FtpRemoteFolder = "/DCIM"
                };
                File.WriteAllText(ConfigPath, JsonSerializer.Serialize(saved));

                MainViewModel vm = MainViewModelProofTests.CreateViewModel();
                InvokeLoadConfig(vm);

                Assert.Equal(legacySecret, vm.FtpPass);

                string diskJson = File.ReadAllText(ConfigPath);
                Assert.DoesNotContain(legacySecret, diskJson, StringComparison.Ordinal);

                var reloaded = JsonSerializer.Deserialize<AppConfig>(diskJson);
                Assert.NotNull(reloaded);
                Assert.True(string.IsNullOrEmpty(reloaded!.FtpPass));
            }
            finally
            {
                RestoreConfig(backup);
            }
        }

        private static void InvokeLoadConfig(MainViewModel vm)
        {
            MethodInfo? load = typeof(MainViewModel).GetMethod("LoadConfig", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(load);
            load.Invoke(vm, null);
        }

        private static string? BackupConfigIfPresent()
        {
            if (!File.Exists(ConfigPath))
            {
                return null;
            }

            string backup = ConfigPath + ".testbak." + Guid.NewGuid().ToString("N");
            File.Copy(ConfigPath, backup);
            return backup;
        }

        private static void RestoreConfig(string? backup)
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    File.Delete(ConfigPath);
                }

                if (backup != null && File.Exists(backup))
                {
                    File.Move(backup, ConfigPath);
                }
            }
            catch
            {
                // Best-effort restore — tests must not fail on cleanup.
            }
        }
    }
}
