#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using QuickMediaIngest.Core.Settings;
using QuickMediaIngest.Localization;

namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel
    {
        [ObservableProperty] private string settingsSearchQuery = string.Empty;

        public bool SettingsSectionAppearanceVisible =>
            SettingsSearch.Matches(SettingsSearchQuery, AppearanceSearchHaystack());

        public bool SettingsSectionDestinationVisible =>
            SettingsSearch.Matches(SettingsSearchQuery, DestinationSearchHaystack());

        public bool SettingsSectionNamingVisible =>
            SettingsSearch.Matches(SettingsSearchQuery, NamingSearchHaystack());

        public bool SettingsSectionLanguageVisible =>
            SettingsSearch.Matches(SettingsSearchQuery, LanguageSearchHaystack());

        public bool SettingsSectionImportVisible =>
            SettingsSearch.Matches(SettingsSearchQuery, ImportSearchHaystack());

        public bool SettingsSearchNoResults =>
            !SettingsSearch.IsBlank(SettingsSearchQuery)
            && !SettingsSectionAppearanceVisible
            && !SettingsSectionDestinationVisible
            && !SettingsSectionNamingVisible
            && !SettingsSectionLanguageVisible
            && !SettingsSectionImportVisible;

        partial void OnSettingsSearchQueryChanged(string value)
        {
            _ = value;
            RefreshSettingsSearch();
            if (SettingsSearch.IsBlank(SettingsSearchQuery))
            {
                return;
            }

            SettingsPrefsAppearanceExpanded = SettingsSectionAppearanceVisible;
            SettingsPrefsDestinationExpanded = SettingsSectionDestinationVisible;
            SettingsPrefsNamingExpanded = SettingsSectionNamingVisible;
            SettingsPrefsLanguageExpanded = SettingsSectionLanguageVisible;
            SettingsPrefsImportSettingsExpanded = SettingsSectionImportVisible;
        }

        private void RefreshSettingsSearch()
        {
            OnPropertyChanged(nameof(SettingsSectionAppearanceVisible));
            OnPropertyChanged(nameof(SettingsSectionDestinationVisible));
            OnPropertyChanged(nameof(SettingsSectionNamingVisible));
            OnPropertyChanged(nameof(SettingsSectionLanguageVisible));
            OnPropertyChanged(nameof(SettingsSectionImportVisible));
            OnPropertyChanged(nameof(SettingsSearchNoResults));
        }

        private static string[] AppearanceSearchHaystack() => new[]
        {
            AppLocalizer.Get("Settings_Section_Appearance"),
            AppLocalizer.Get("Theme_Label"),
            AppLocalizer.Get("Settings_OpenAppInfo"),
        };

        private static string[] DestinationSearchHaystack() => new[]
        {
            AppLocalizer.Get("Settings_SaveDestination"),
            AppLocalizer.Get("Settings_DestinationPreset"),
        };

        private static string[] NamingSearchHaystack() => new[]
        {
            AppLocalizer.Get("Settings_FileNamingScheme"),
            AppLocalizer.Get("Settings_Preset"),
            AppLocalizer.Get("Settings_DateFormat"),
            AppLocalizer.Get("Settings_TimeFormat"),
            AppLocalizer.Get("Settings_Separator"),
        };

        private static string[] LanguageSearchHaystack() => new[]
        {
            AppLocalizer.Get("Settings_Section_Language"),
            AppLocalizer.Get("Settings_DisplayLanguage"),
            AppLocalizer.Get("Settings_SaveCrashDetailsHint"),
        };

        private static string[] ImportSearchHaystack() => new[]
        {
            AppLocalizer.Get("Settings_Section_ImportSettings"),
            AppLocalizer.Get("Settings_ImportBehaviorSection"),
            AppLocalizer.Get("Settings_DuplicateHandling"),
            AppLocalizer.Get("Settings_VerificationMode"),
            AppLocalizer.Get("Settings_ThumbnailPerformance"),
            AppLocalizer.Get("Settings_PreferAdbTransferHint"),
        };
    }
}
