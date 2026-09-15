#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickMediaIngest.Core.ImportUi;
using QuickMediaIngest.Localization;

namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel
    {
        public bool ShowImportSceneOpenDestination =>
            ImportProgressScene.ShowOpenDestination(IsImporting, DestinationRoot);

        public bool DimShootListForImport => ImportProgressScene.DimList(IsImporting);

        public string ImportSceneTitle => IsImporting
            ? AppLocalizer.Get("Import_InProgressTitle")
            : AppLocalizer.Get("Import_SceneDoneTitle");

        public string ImportScenePercentText =>
            string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0}%", ImportProgressScene.ClampPercent(ProgressPercent));

        partial void OnIsImportingChanged(bool value)
        {
            _ = value;
            SyncIngestBench();
            RefreshImportSceneHints();
        }

        partial void OnProgressPercentChanged(int value)
        {
            _ = value;
            OnPropertyChanged(nameof(ImportScenePercentText));
        }

        internal void RefreshImportSceneHints()
        {
            OnPropertyChanged(nameof(ShowImportSceneOpenDestination));
            OnPropertyChanged(nameof(DimShootListForImport));
            OnPropertyChanged(nameof(ImportSceneTitle));
            OnPropertyChanged(nameof(ImportScenePercentText));
        }

        [RelayCommand]
        private void DismissImportProgressScene()
        {
            ShowImportProgressDialog = false;
        }
    }
}
