#nullable enable
using System;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Chrome;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Nav;
using QuickMediaIngest.Localization;

namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel
    {
        private readonly OverlayNav _overlayNav = new();

        public OverlayNav OverlayNav => _overlayNav;

        [ObservableProperty] private ItemGroup? selectedGroup;
        [ObservableProperty] private bool showNotificationsFlyout;
        [ObservableProperty] private bool viewToolbarExpanded;
        [ObservableProperty] private bool commandBarCompact;

        public bool ShowDestinationChipInPrimaryBar => !CommandBarCompact;

        public string ViewOverflowHeader => CommandBarCompact
            ? AppLocalizer.Get("Toolbar_ViewOverflow")
            : AppLocalizer.Get("Toolbar_ViewSection");

        public void ApplyWindowWidth(double width)
        {
            bool compact = CommandBarOverflow.IsCompact(width);
            if (compact == CommandBarCompact)
            {
                return;
            }

            CommandBarCompact = compact;
            OnPropertyChanged(nameof(ShowDestinationChipInPrimaryBar));
            OnPropertyChanged(nameof(ViewOverflowHeader));
        }

        partial void OnCommandBarCompactChanged(bool value)
        {
            _ = value;
            OnPropertyChanged(nameof(ShowDestinationChipInPrimaryBar));
            OnPropertyChanged(nameof(ViewOverflowHeader));
        }

        public bool ShowToolbarRetryFailed => FailedImportRecords.Count > 0;
        public bool ShowToolbarResumePending => HasPendingImportPlan;
        public bool HasSelectedImportFiles => Groups.SelectMany(g => g.Items).Any(i => i.IsSelected);
        public bool ShowToolbarQueue => QueuedImportCount > 0 || HasSelectedImportFiles;
        public bool ShowToolbarRebuildPreviews => Groups.Count > 0;
        public bool ShowGroupDetailsPanel => SelectedGroup != null;

        internal void RefreshToolbarActionHints()
        {
            OnPropertyChanged(nameof(ShowToolbarRetryFailed));
            OnPropertyChanged(nameof(ShowToolbarResumePending));
            OnPropertyChanged(nameof(HasSelectedImportFiles));
            OnPropertyChanged(nameof(ShowToolbarQueue));
            OnPropertyChanged(nameof(ShowToolbarRebuildPreviews));
            OnPropertyChanged(nameof(ShowGroupDetailsPanel));
        }

        partial void OnSelectedGroupChanged(ItemGroup? value) => OnPropertyChanged(nameof(ShowGroupDetailsPanel));
        partial void OnHasPendingImportPlanChanged(bool value) => OnPropertyChanged(nameof(ShowToolbarResumePending));
        partial void OnQueuedImportCountChanged(int value) => OnPropertyChanged(nameof(ShowToolbarQueue));

        [RelayCommand]
        private void ToggleNotificationsFlyout() => ShowNotificationsFlyout = !ShowNotificationsFlyout;

        private void SyncOverlayFlags()
        {
            ShowSettingsDialog = _overlayNav.IsVisible(OverlayId.Settings);
            ShowAboutDialog = _overlayNav.IsVisible(OverlayId.About);
            ShowFeedbackDialog = _overlayNav.IsVisible(OverlayId.Feedback);
        }

        [RelayCommand]
        private void OpenAboutFromSettings()
        {
            _overlayNav.OpenAboutFromSettings();
            SyncOverlayFlags();
        }

        private void PushOverlay(OverlayId id)
        {
            _overlayNav.Push(id);
            SyncOverlayFlags();
        }

        private bool PopOverlay()
        {
            if (!_overlayNav.CanPop)
            {
                return false;
            }

            _overlayNav.Pop();
            SyncOverlayFlags();
            return true;
        }

        [RelayCommand]
        private void OpenImportHistory()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _overlayNav.Reset();
                    SyncOverlayFlags();
                    ShowScanExclusionsPanel = false;
                    ShowImportHistoryDialog = true;
                });
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "OpenImportHistory UI activation failed.");
            }
        }

        [RelayCommand]
        private void CloseImportHistory() => ShowImportHistoryDialog = false;

        [RelayCommand]
        private void ToggleSettings()
        {
            ShowScanExclusionsPanel = false;
            ShowImportHistoryDialog = false;
            if (_overlayNav.IsVisible(OverlayId.Settings))
            {
                SettingsSearchQuery = string.Empty;
                PopOverlay();
                return;
            }

            PushOverlay(OverlayId.Settings);
        }

        [RelayCommand]
        private void OpenScanExclusions()
        {
            _overlayNav.Reset();
            SyncOverlayFlags();
            ShowImportHistoryDialog = false;
            ShowScanExclusionsPanel = true;
        }

        [RelayCommand]
        private void CloseScanExclusions() => ShowScanExclusionsPanel = false;
    }
}
