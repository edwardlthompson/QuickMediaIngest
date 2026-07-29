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
        public void LoadConfig_PreservesCustomDestinationAndNamingTemplate()
        {
            WpfTestHost.EnsureInitialized();
            string? backup = BackupConfigIfPresent();

            try
            {
                string folder = Path.GetDirectoryName(ConfigPath)!;
                Directory.CreateDirectory(folder);

                const string customDest = @"D:\MEDIA\01 - Unedited";
                const string customTemplate = "[Date]_[Time]_[Sequence]_[ShootName]";
                var saved = new AppConfig
                {
                    DestinationRoot = customDest,
                    DestinationPreset = "Custom",
                    LastSessionDestinationRoot = customDest,
                    NamingTemplate = customTemplate,
                    // Mismatched preset label must not wipe the saved template on load.
                    NamingPreset = "Recommended (Date + Shoot + Original)",
                    NamingDateFormat = "yyyy-MM-dd",
                    NamingTimeFormat = "HH-mm-ss",
                    NamingSeparator = "_",
                    NamingIncludeSequence = true,
                    NamingShootNameSample = "my-shoot",
                    NamingLowercase = true
                };
                File.WriteAllText(ConfigPath, JsonSerializer.Serialize(saved));

                MainViewModel vm = MainViewModelProofTests.CreateViewModel();
                InvokeLoadConfig(vm);
                InvokeSyncNamingAndCoerce(vm);

                Assert.Equal(customDest, vm.DestinationRoot);
                Assert.Equal("Custom", vm.DestinationPreset);
                Assert.Equal(customTemplate, vm.NamingTemplate);
                Assert.Equal("Custom", vm.NamingPreset);
                Assert.True(vm.NamingIncludeTime);
                Assert.True(vm.NamingIncludeSequence);
                Assert.False(vm.NamingIncludeOriginalName);
            }
            finally
            {
                RestoreConfig(backup);
            }
        }

        [Fact]
        public void SaveConfig_ThenLoadConfig_RoundTripsDestinationAndNaming()
        {
            WpfTestHost.EnsureInitialized();
            string? backup = BackupConfigIfPresent();

            try
            {
                const string customDest = @"E:\Shoots\Inbox";
                MainViewModel vm = MainViewModelProofTests.CreateViewModel();
                vm.DestinationPreset = "Custom";
                vm.DestinationRoot = customDest;
                vm.NamingIncludeDate = true;
                vm.NamingIncludeTime = true;
                vm.NamingIncludeSequence = true;
                vm.NamingIncludeShootName = true;
                vm.NamingIncludeOriginalName = false;
                vm.NamingSeparator = "_";
                // Allow checkbox-driven template rebuild + Custom coerce.
                Assert.Equal("[Date]_[Time]_[Sequence]_[ShootName]", vm.NamingTemplate);
                Assert.Equal("Custom", vm.NamingPreset);
                vm.SaveConfig();

                MainViewModel reloaded = MainViewModelProofTests.CreateViewModel();
                InvokeLoadConfig(reloaded);
                InvokeSyncNamingAndCoerce(reloaded);

                Assert.Equal(customDest, reloaded.DestinationRoot);
                Assert.Equal("Custom", reloaded.DestinationPreset);
                Assert.Equal("[Date]_[Time]_[Sequence]_[ShootName]", reloaded.NamingTemplate);
                Assert.Equal("Custom", reloaded.NamingPreset);
            }
            finally
            {
                RestoreConfig(backup);
            }
        }

        [Fact]
        public void RefreshDestinationPresetLabels_RestoresSelectedPreset()
        {
            WpfTestHost.EnsureInitialized();
            MainViewModel vm = MainViewModelProofTests.CreateViewModel();
            vm.DestinationPreset = "LastSession";
            vm.LastSessionDestinationRoot = Path.GetTempPath();

            MethodInfo? refresh = typeof(MainViewModel).GetMethod(
                "RefreshDestinationPresetLabels",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(refresh);
            refresh.Invoke(vm, null);

            Assert.Equal("LastSession", vm.DestinationPreset);
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

        private static void InvokeSyncNamingAndCoerce(MainViewModel vm)
        {
            MethodInfo? sync = typeof(MainViewModel).GetMethod(
                "SyncNamingOptionsFromTemplate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(sync);
            sync.Invoke(vm, null);

            MethodInfo? coerce = typeof(MainViewModel).GetMethod(
                "CoerceNamingPresetToCustomIfTemplateDiverged",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(coerce);
            coerce.Invoke(vm, null);
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
