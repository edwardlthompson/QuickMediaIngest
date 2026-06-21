#nullable enable
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using QuickMediaIngest;
using QuickMediaIngest.ViewModels;
using Xunit;

namespace QuickMediaIngest.Tests
{
    /// <summary>
    /// Headless substitutes for BUILD_PLAN HUMAN visual spot-checks (Preferences + toolbar slider bindings).
    /// </summary>
    [Collection("Wpf")]
    public class HumanSignoffVerificationTests
    {
        private static string ConfigPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest", "config.json");

        [Fact]
        public void AfterConfigReload_DeleteAfterImportBindingReflectsPersistedState()
        {
            WpfTestHost.EnsureInitialized();
            string? backup = BackupConfigIfPresent();

            try
            {
                WriteConfig(new AppConfig
                {
                    DeleteAfterImport = true,
                    DeleteAfterImportPromptDismissed = true,
                    ThumbnailSize = 200
                });

                WpfTestHost.RunOnUiThread(() =>
                {
                    MainViewModel vm = MainViewModelProofTests.CreateViewModel();
                    InvokeLoadConfig(vm);

                    var checkbox = new CheckBox();
                    checkbox.SetBinding(CheckBox.IsCheckedProperty, new Binding(nameof(MainViewModel.DeleteAfterImport))
                    {
                        Source = vm,
                        Mode = BindingMode.TwoWay
                    });
                    checkbox.UpdateLayout();

                    Assert.True(checkbox.IsChecked);
                    Assert.True(vm.DeleteAfterImport);
                    Assert.True(vm.DeleteAfterImportPromptDismissed);
                });
            }
            finally
            {
                RestoreConfig(backup);
            }
        }

        [Fact]
        public void AfterConfigReload_ThumbnailSliderBindingShowsPersistedZoom()
        {
            WpfTestHost.EnsureInitialized();
            string? backup = BackupConfigIfPresent();

            try
            {
                WriteConfig(new AppConfig { ThumbnailSize = 200 });

                WpfTestHost.RunOnUiThread(() =>
                {
                    MainViewModel vm = MainViewModelProofTests.CreateViewModel();
                    InvokeLoadConfig(vm);

                    var slider = new Slider { Minimum = 50, Maximum = 300 };
                    slider.SetBinding(Slider.ValueProperty, new Binding(nameof(MainViewModel.ThumbnailSize))
                    {
                        Source = vm,
                        Mode = BindingMode.TwoWay
                    });
                    slider.UpdateLayout();

                    Assert.Equal(200, vm.ThumbnailSize);
                    Assert.Equal(200, slider.Value);
                });
            }
            finally
            {
                RestoreConfig(backup);
            }
        }

        [Fact]
        public void SimulatedRestart_DeleteAfterImportEnabled_NoDialogWhenPreviouslyDismissed()
        {
            WpfTestHost.EnsureInitialized();
            MainViewModel vm = MainViewModelProofTests.CreateViewModel();
            vm.DeleteAfterImport = true;
            vm.DeleteAfterImportPromptDismissed = true;

            bool userInitiated = false;
            bool reverted = false;
            QuickMediaIngest.Services.DeleteAfterImportConfirmHelper.HandleChecked(
                vm,
                ref userInitiated,
                () => reverted = true);

            Assert.False(userInitiated);
            Assert.False(reverted);
            Assert.True(vm.DeleteAfterImport);
        }

        private static void WriteConfig(AppConfig config)
        {
            string folder = Path.GetDirectoryName(ConfigPath)!;
            Directory.CreateDirectory(folder);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config));
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
                // Best-effort restore.
            }
        }
    }
}
