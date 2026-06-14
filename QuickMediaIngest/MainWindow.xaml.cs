using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using DragEventArgs = System.Windows.DragEventArgs;
using Point = System.Windows.Point;
using System.Windows.Input;
using System.Windows.Media;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;
using DataObject = System.Windows.DataObject;
using DragDropEffects = System.Windows.DragDropEffects;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Automation;
using System.Windows.Media.Animation;
using System.Threading;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Localization;
using QuickMediaIngest.ViewModels;
using QuickMediaIngest.Services;

namespace QuickMediaIngest
{
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger;
        private const string RibbonTileDragFormat = "QuickMediaIngest.RibbonTile";
        private bool _ribbonLayoutRefreshQueued;
        private Point _ribbonTileDragStartPoint;
        private bool _isRibbonTileDragInProgress;
        private Border? _activeRibbonDraggedTile;
        private int _activeRibbonPreviewIndex = -1;
        private double _savedShootGroupsScrollOffset;
        private bool _isRestoringWindowState;
        private bool _deleteAfterImportUserInitiated;

        public Visual BlurBackdropSource => MainChromeRoot;

        public MainWindow(MainViewModel viewModel, ILogger<MainWindow> logger)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.GroupsListRebuildStarting += ViewModel_GroupsListRebuildStarting;
            viewModel.GroupsListRebuildCompleted += ViewModel_GroupsListRebuildCompleted;
            Closed += MainWindow_OnClosed;
            _logger = logger;
            _logger.LogInformation("Main window initialized.");

            // Initialize theme toggle state
            try
            {
                if (ThemeToggle != null)
                {
                    ThemeToggle.IsChecked = App.CurrentIsDarkTheme;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not sync theme toggle at startup.");
            }
        }

        private void MainWindow_OnClosed(object? sender, EventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.GroupsListRebuildStarting -= ViewModel_GroupsListRebuildStarting;
                    vm.GroupsListRebuildCompleted -= ViewModel_GroupsListRebuildCompleted;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not unsubscribe group rebuild handlers.");
            }
        }

        private static ScrollViewer? FindDescendantScrollViewer(DependencyObject? root)
        {
            if (root == null)
            {
                return null;
            }

            if (root is ScrollViewer sv)
            {
                return sv;
            }

            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                ScrollViewer? found = FindDescendantScrollViewer(VisualTreeHelper.GetChild(root, i));
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private double TryGetShootGroupsVerticalOffset()
        {
            try
            {
                if (GroupsListView == null)
                {
                    return 0;
                }

                ScrollViewer? scroll = FindDescendantScrollViewer(GroupsListView);
                return scroll?.VerticalOffset ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not read shoot list scroll offset.");
                return 0;
            }
        }

        private void TryRestoreShootGroupsVerticalOffset(double offset)
        {
            try
            {
                if (GroupsListView == null)
                {
                    return;
                }

                ScrollViewer? scroll = FindDescendantScrollViewer(GroupsListView);
                if (scroll != null && offset >= 0)
                {
                    scroll.ScrollToVerticalOffset(offset);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not restore shoot list scroll offset.");
            }
        }

        private void ViewModel_GroupsListRebuildStarting(object? sender, EventArgs e)
        {
            _savedShootGroupsScrollOffset = TryGetShootGroupsVerticalOffset();
        }

        private void ViewModel_GroupsListRebuildCompleted(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(
                new Action(() => TryRestoreShootGroupsVerticalOffset(_savedShootGroupsScrollOffset)),
                DispatcherPriority.Loaded);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Deferred from constructor: avoids extra work on the critical path during XAML/build of the logical tree.
            try
            {
                var showOnLaunch = Environment.GetEnvironmentVariable("QMI_SHOW_FTP_ON_LAUNCH");
                if (!string.IsNullOrEmpty(showOnLaunch) && showOnLaunch == "1" && DataContext is MainViewModel vm)
                {
                    vm.ShowAddFtpDialog = true;
                    _logger.LogInformation("Debug: showing Add FTP dialog on launch due to QMI_SHOW_FTP_ON_LAUNCH=1");
                }
            }
            catch
            {
                // Ignore debug launcher failures.
            }

            try
            {
                ApplySidebarCollapsedChrome(SidebarCollapseToggle?.IsChecked == true);
            }
            catch
            {
                // Ignore sidebar chrome timing during startup.
            }

            try
            {
                if (LiveAnnouncementHost != null)
                {
                    AutomationProperties.SetLiveSetting(LiveAnnouncementHost, AutomationLiveSetting.Polite);
                    AutomationProperties.SetName(LiveAnnouncementHost, AppLocalizer.Get("A11y_StatusAnnouncements"));
                }

                if (ThemeToggle != null)
                {
                    AutomationProperties.SetName(ThemeToggle, AppLocalizer.Get("A11y_ThemeToggle"));
                }

                if (SidebarCollapseToggle != null)
                {
                    AutomationProperties.SetName(SidebarCollapseToggle, AppLocalizer.Get("A11y_SidebarCollapse"));
                }
            }
            catch
            {
                // Ignore platforms where live regions are unavailable.
            }
        }

        internal void AddFtpOverlay_Loaded(object sender, RoutedEventArgs e)
        {
            _logger?.LogInformation("AddFtpOverlay loaded on thread {ThreadId}.", Thread.CurrentThread.ManagedThreadId);
        }

        internal void AddFtpOverlay_Unloaded(object sender, RoutedEventArgs e)
        {
            _logger?.LogInformation("AddFtpOverlay unloaded on thread {ThreadId}.", Thread.CurrentThread.ManagedThreadId);
        }

        public void ApplyWindowStateFromViewModel()
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            _isRestoringWindowState = true;
            try
            {
                double width = Math.Max(WindowStateHelper.MinWidth, vm.SavedWindowWidth);
                double height = Math.Max(WindowStateHelper.MinHeight, vm.SavedWindowHeight);
                bool hasSavedPosition = vm.SavedWindowLeft.HasValue && vm.SavedWindowTop.HasValue;

                if (hasSavedPosition)
                {
                    var workingAreas = WindowStateHelper.GetAllWorkingAreas();
                    var clamped = WindowStateHelper.ClampToVisibleBounds(
                        width,
                        height,
                        vm.SavedWindowLeft!.Value,
                        vm.SavedWindowTop!.Value,
                        workingAreas);

                    width = clamped.Width;
                    height = clamped.Height;
                    WindowStartupLocation = WindowStartupLocation.Manual;
                    Left = clamped.Left;
                    Top = clamped.Top;
                }

                Width = width;
                Height = height;

                if (vm.SavedWindowMaximized)
                {
                    WindowState = WindowState.Maximized;
                }
                else
                {
                    WindowState = WindowState.Normal;
                }
            }
            finally
            {
                _isRestoringWindowState = false;
            }
        }

        private void PersistWindowState()
        {
            if (_isRestoringWindowState || DataContext is not MainViewModel vm)
            {
                return;
            }

            var bounds = WindowStateHelper.GetBoundsToPersist(this);
            vm.SaveWindowState(bounds.Width, bounds.Height, bounds.Maximized, bounds.Left, bounds.Top);
        }

        private void MainContent_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm || RibbonTilePanel == null)
                return;

            var order = vm.RibbonTileOrder;
            if (order == null || order.Count == 0)
                return;

            // Build a lookup of tag -> tile
            var tilesByTag = RibbonTilePanel.Children
                .OfType<System.Windows.Controls.Border>()
                .Where(b => b.Tag is string)
                .ToDictionary(b => (string)b.Tag!);

            // Only reorder if all saved tags are still present
            if (!order.All(t => tilesByTag.ContainsKey(t)))
                return;

            RibbonTilePanel.Children.Clear();
            foreach (var tag in order)
                RibbonTilePanel.Children.Add(tilesByTag[tag]);

            // Add any tiles not in the saved order (new tiles added in future versions)
            foreach (var tile in tilesByTag.Values)
                if (!RibbonTilePanel.Children.Contains(tile))
                    RibbonTilePanel.Children.Add(tile);
        }

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (DataContext is MainViewModel vm)
                {
                    double newSize = vm.ThumbnailSize + (e.Delta > 0 ? 10 : -10);
                    if (newSize >= 50 && newSize <= 300)
                    {
                        vm.ThumbnailSize = newSize;
                    }
                    e.Handled = true;
                }
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_ribbonLayoutRefreshQueued)
            {
                return;
            }

            if (WindowState == WindowState.Normal && !_isRestoringWindowState)
                PersistWindowState();

            _ribbonLayoutRefreshQueued = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _ribbonLayoutRefreshQueued = false;
                RibbonTilePanel?.InvalidateMeasure();
                RibbonTilePanel?.InvalidateArrange();
                RibbonTilePanel?.UpdateLayout();
            }), DispatcherPriority.Render);
        }

        private void RibbonTileHandle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _ribbonTileDragStartPoint = e.GetPosition(this);
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (_isRestoringWindowState)
            {
                return;
            }

            PersistWindowState();
        }
    }
}
