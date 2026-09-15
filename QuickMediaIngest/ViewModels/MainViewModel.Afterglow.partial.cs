#nullable enable
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickMediaIngest.Core.ImportUi;
using QuickMediaIngest.Localization;

namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel
    {
        [ObservableProperty] private bool showImportAfterglow;
        [ObservableProperty] private string importAfterglowText = string.Empty;
        [ObservableProperty] private string importAfterglowDestination = string.Empty;

        public string DestinationChipLabel => ImportAfterglow.FolderLabel(DestinationRoot);

        private void ShowImportAfterglowBanner(int importedCount, int failedCount)
        {
            string folder = ImportAfterglow.FolderLabel(DestinationRoot);
            ImportAfterglowDestination = DestinationRoot ?? string.Empty;
            ImportAfterglowText = failedCount > 0
                ? AppLocalizer.Format("ImportAfterglow_TextWarning", importedCount, folder, failedCount)
                : AppLocalizer.Format("ImportAfterglow_Text", importedCount, folder);
            ShowImportAfterglow = true;
            try
            {
                System.Media.SystemSounds.Exclamation.Play();
            }
            catch
            {
                // Ignore local sound playback issues.
            }
        }

        [RelayCommand]
        private void OpenImportAfterglowFolder()
        {
            if (string.IsNullOrWhiteSpace(ImportAfterglowDestination))
            {
                return;
            }

            try
            {
                _shellService.OpenFolder(ImportAfterglowDestination);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Open afterglow folder failed.");
            }
        }

        [RelayCommand]
        private void DismissImportAfterglow()
        {
            ShowImportAfterglow = false;
        }
    }
}
