#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Nav;
using QuickMediaIngest.Localization;

namespace QuickMediaIngest.Desktop;

public partial class MainWindow
{
    private void Prefs_Click(object? sender, RoutedEventArgs e)
    {
        Host.Model.Prefs.ApplySearch();
        ShowOverlay(OverlayId.Settings);
    }

    private void PrefsSearch_LostFocus(object? sender, RoutedEventArgs e) =>
        Host.Model.Prefs.ApplySearch();

    private void PrefsTheme_Changed(object? sender, SelectionChangedEventArgs e) => ApplyPrefsTheme();

    private void PrefsLanguage_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (!_viewReady)
        {
            return;
        }

        AppLocalizer.SetCulture(Bench.Prefs.Language);
        ApplyLocalizedChrome();
        ApplyA11yChrome();
    }

    private void PrefsAbout_Click(object? sender, RoutedEventArgs e)
    {
        IngestBenchAbout.Open(Host);
        RefreshOverlays();
    }

    private void PrefsClose_Click(object? sender, RoutedEventArgs e)
    {
        IngestBenchPrefs.Save(Host);
        IngestBenchNaming.Save(Host);
        CloseOverlay_Click(sender, e);
    }

    private void AboutDonate_Click(object? sender, RoutedEventArgs e) =>
        IngestBenchAbout.OpenDonate();

    private async void AboutCheck_Click(object? sender, RoutedEventArgs e) =>
        await IngestBenchAbout.CheckNowAsync(Host, LocCopy);

    private async void PrefsExport_Click(object? sender, RoutedEventArgs e)
    {
        IStorageFile? file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = AppLocalizer.Get("Settings_MainTitle"),
                SuggestedFileName = "qmi-prefs.json",
                DefaultExtension = "json",
            });
        if (file?.TryGetLocalPath() is string local && !string.IsNullOrWhiteSpace(local))
        {
            await File.WriteAllTextAsync(local, IngestBenchPrefs.ExportJson(Host));
        }
    }

    private async void PrefsImport_Click(object? sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = AppLocalizer.Get("Settings_MainTitle"),
            });
        if (files.Count == 0)
        {
            return;
        }

        string? local = files[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(local) || !File.Exists(local))
        {
            return;
        }

        IngestBenchPrefs.ImportJson(Host, await File.ReadAllTextAsync(local));
        ApplyPrefsTheme();
        AppLocalizer.SetCulture(Bench.Prefs.Language);
        ApplyLocalizedChrome();
    }

    private void PrefsNamingPreset_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (!_viewReady || Bench.Naming.Suppress)
        {
            return;
        }

        IngestBenchNaming.ApplyPreset(Host, Bench.Naming.Preset);
    }

    private void PrefsNamingOptions_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is CheckBox box && box.Tag is string field)
        {
            IngestBenchNaming.ToggleFile(Host, field, box.IsChecked == true);
        }
    }

    private void PrefsNamingOptions_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (!_viewReady || Bench.Naming.Suppress)
        {
            return;
        }

        IngestBenchNaming.ApplyOptions(Host);
    }

    private void PrefsShootSample_LostFocus(object? sender, RoutedEventArgs e) =>
        IngestBenchNaming.RefreshPreview(Host);

    private void PrefsLowercase_Click(object? sender, RoutedEventArgs e) =>
        IngestBenchNaming.RefreshPreview(Host);

    private void PrefsDestPreset_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (!_viewReady || string.Equals(Bench.Prefs.DestinationPreset, "Custom", System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        IngestBenchDest.ApplyPreset(Host, Bench.Prefs.DestinationPreset);
    }

    private void PrefsFolderOptions_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is CheckBox box && box.Tag is string token)
        {
            IngestBenchNaming.ToggleFolder(Host, token, box.IsChecked == true);
        }
    }

    private static string? ChipToken(object? sender) =>
        sender is Button button ? button.Tag as string ?? button.Content as string : null;

    private void NamingToken_Click(object? sender, RoutedEventArgs e) =>
        IngestBenchNaming.InsertFileToken(Host, ChipToken(sender));

    private void NamingTokenRemove_Click(object? sender, RoutedEventArgs e) =>
        IngestBenchNaming.RemoveFileToken(Host, ChipToken(sender));

    private void FolderToken_Click(object? sender, RoutedEventArgs e) =>
        IngestBenchNaming.InsertFolderToken(Host, ChipToken(sender));

    private void ApplyPrefsTheme()
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = string.Equals(Bench.Prefs.Theme, "Light", System.StringComparison.OrdinalIgnoreCase)
            ? ThemeVariant.Light
            : ThemeVariant.Dark;
    }
}
