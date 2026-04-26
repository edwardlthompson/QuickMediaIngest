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
using System.Windows.Media.Animation;
using System.Threading;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.ViewModels;

namespace QuickMediaIngest
{
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger;
        private const string TokenDragFormat = "QuickMediaIngest.TokenPayload";
        private const string RibbonTileDragFormat = "QuickMediaIngest.RibbonTile";
        private Point _tokenDragStartPoint;
        private bool _isTokenDragInProgress;
        private bool _ribbonLayoutRefreshQueued;
        private Point _ribbonTileDragStartPoint;
        private bool _isRibbonTileDragInProgress;
        private Border? _activeRibbonDraggedTile;
        private int _activeRibbonPreviewIndex = -1;
        public MainWindow(MainViewModel viewModel, ILogger<MainWindow> logger)
        {
            InitializeComponent();
            DataContext = viewModel;
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
            catch { }

            // Loaded += MainWindow_Loaded;
            try
            {
                var showOnLaunch = Environment.GetEnvironmentVariable("QMI_SHOW_FTP_ON_LAUNCH");
                if (!string.IsNullOrEmpty(showOnLaunch) && showOnLaunch == "1" && DataContext is MainViewModel vm)
                {
                    vm.ShowAddFtpDialog = true;
                    _logger.LogInformation("Debug: showing Add FTP dialog on launch due to QMI_SHOW_FTP_ON_LAUNCH=1");
                    try
                    {
                        // Force the overlay visible in case binding/visual tree timing prevents it from showing
                        AddFtpOverlay.Visibility = System.Windows.Visibility.Visible;
                        AddFtpOverlay.BringIntoView();
                    }
                    catch { }
                }
            }
            catch { }
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // OnboardingDialog removed
        }

        private void AddFtpOverlay_Loaded(object sender, RoutedEventArgs e)
        {
            _logger?.LogInformation("AddFtpOverlay loaded on thread {ThreadId}.", Thread.CurrentThread.ManagedThreadId);
        }

        private void AddFtpOverlay_Unloaded(object sender, RoutedEventArgs e)
        {
            _logger?.LogInformation("AddFtpOverlay unloaded on thread {ThreadId}.", Thread.CurrentThread.ManagedThreadId);
        }

        public void ApplyWindowStateFromViewModel()
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            Width = vm.SavedWindowWidth;
            Height = vm.SavedWindowHeight;
            if (vm.SavedWindowLeft.HasValue && vm.SavedWindowTop.HasValue)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = vm.SavedWindowLeft.Value;
                Top = vm.SavedWindowTop.Value;
            }
            if (vm.SavedWindowMaximized)
            {
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowState = WindowState.Normal;
            }
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
                    if (newSize >= 80 && newSize <= 250) // Bounds check matching Slider
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

                if (WindowState == WindowState.Normal && DataContext is MainViewModel vmSize)
                    vmSize.SaveWindowState(Width, Height, false, Left, Top);

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
            if (DataContext is not MainViewModel vm) return;

            if (WindowState == WindowState.Maximized)
                vm.SaveWindowState(vm.SavedWindowWidth, vm.SavedWindowHeight, true);
            else if (WindowState == WindowState.Normal)
                vm.SaveWindowState(Width, Height, false, Left, Top);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }
            if (WindowState == WindowState.Maximized)
            {
                vm.SaveWindowState(vm.SavedWindowWidth, vm.SavedWindowHeight, true);
            }
            else
            {
                vm.SaveWindowState(Width, Height, false, Left, Top);
            }
        }

        private void RibbonTileHandle_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isRibbonTileDragInProgress || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            var currentPosition = e.GetPosition(this);
            if (Math.Abs(currentPosition.X - _ribbonTileDragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(currentPosition.Y - _ribbonTileDragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            var tile = GetRibbonTile(sender as DependencyObject);
            if (tile == null)
            {
                return;
            }

            var data = new System.Windows.DataObject(RibbonTileDragFormat, tile);

            try
            {
                _isRibbonTileDragInProgress = true;
                _activeRibbonDraggedTile = tile;
                System.Windows.DragDrop.DoDragDrop(tile, data, System.Windows.DragDropEffects.Move);
            }
            finally
            {
                _isRibbonTileDragInProgress = false;
                _activeRibbonDraggedTile = null;
                _activeRibbonPreviewIndex = -1;
                ClearRibbonTileOffsets();
            }
        }

        private void RibbonTilePanel_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(RibbonTileDragFormat) || RibbonTilePanel == null)
            {
                e.Effects = System.Windows.DragDropEffects.None;
                e.Handled = true;
                return;
            }

            e.Effects = System.Windows.DragDropEffects.Move;

            if (e.Data.GetData(RibbonTileDragFormat) is Border draggedTile)
            {
                int dropIndex = GetRibbonDropIndex(e.GetPosition(RibbonTilePanel), draggedTile);
                if (dropIndex != _activeRibbonPreviewIndex)
                {
                    _activeRibbonPreviewIndex = dropIndex;
                    PreviewRibbonTileReorderAtIndex(draggedTile, dropIndex);
                }
            }

            e.Handled = true;
        }

        private void RibbonTilePanel_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            _activeRibbonPreviewIndex = -1;

            if (!e.Data.GetDataPresent(RibbonTileDragFormat) || RibbonTilePanel == null)
            {
                ClearRibbonTileOffsets();
                return;
            }

            if (e.Data.GetData(RibbonTileDragFormat) is not Border draggedTile)
            {
                ClearRibbonTileOffsets();
                return;
            }

            int sourceIndex = RibbonTilePanel.Children.IndexOf(draggedTile);
            if (sourceIndex < 0)
            {
                ClearRibbonTileOffsets();
                return;
            }

            int dropIndex = GetRibbonDropIndex(e.GetPosition(RibbonTilePanel), draggedTile);
            int insertIndex = dropIndex;
            if (sourceIndex < insertIndex)
            {
                insertIndex--;
            }

            if (insertIndex < 0) insertIndex = 0;
            if (insertIndex > RibbonTilePanel.Children.Count - 1) insertIndex = RibbonTilePanel.Children.Count - 1;

            if (insertIndex == sourceIndex)
            {
                ClearRibbonTileOffsets();
                return;
            }

            RibbonTilePanel.Children.RemoveAt(sourceIndex);
            RibbonTilePanel.Children.Insert(insertIndex, draggedTile);

            // Persist the new order
            if (DataContext is MainViewModel vm)
            {
                var order = RibbonTilePanel.Children
                    .OfType<System.Windows.Controls.Border>()
                    .Where(b => b.Tag is string)
                    .Select(b => (string)b.Tag!);
                vm.SaveTileOrder(order);
            }

            ClearRibbonTileOffsets();
        }

        private void RibbonTilePanel_DragLeave(object sender, DragEventArgs e)
        {
            _activeRibbonPreviewIndex = -1;
            ClearRibbonTileOffsets();
        }

        private int GetRibbonDropIndex(Point panelPosition, Border draggedTile)
        {
            if (RibbonTilePanel == null)
            {
                return 0;
            }

            var tiles = RibbonTilePanel.Children.OfType<Border>().ToList();
            if (tiles.Count == 0)
            {
                return 0;
            }

            double x = panelPosition.X;

            foreach (var tile in tiles)
            {
                if (tile == draggedTile)
                {
                    continue;
                }

                var tileLeft = tile.TranslatePoint(new Point(0, 0), RibbonTilePanel).X;
                double midpoint = tileLeft + (tile.ActualWidth / 2);
                if (x < midpoint)
                {
                    return RibbonTilePanel.Children.IndexOf(tile);
                }
            }

            return RibbonTilePanel.Children.Count;
        }

        private void PreviewRibbonTileReorderAtIndex(Border draggedTile, int dropIndex)
        {
            if (RibbonTilePanel == null)
            {
                return;
            }

            int sourceIndex = RibbonTilePanel.Children.IndexOf(draggedTile);
            if (sourceIndex < 0)
            {
                ClearRibbonTileOffsets();
                return;
            }

            if (dropIndex < 0) dropIndex = 0;
            if (dropIndex > RibbonTilePanel.Children.Count) dropIndex = RibbonTilePanel.Children.Count;

            if (dropIndex == sourceIndex || dropIndex == sourceIndex + 1)
            {
                ClearRibbonTileOffsets();
                return;
            }

            double shift = draggedTile.ActualWidth;
            if (shift <= 0)
            {
                shift = 140;
            }

            if (draggedTile.Margin.Left > 0)
            {
                shift += draggedTile.Margin.Left;
            }
            if (draggedTile.Margin.Right > 0)
            {
                shift += draggedTile.Margin.Right;
            }

            foreach (var tile in RibbonTilePanel.Children.OfType<Border>())
            {
                if (tile == draggedTile)
                {
                    continue;
                }

                int tileIndex = RibbonTilePanel.Children.IndexOf(tile);
                double targetOffset = 0;

                if (sourceIndex < dropIndex)
                {
                    if (tileIndex > sourceIndex && tileIndex < dropIndex)
                    {
                        targetOffset = -shift;
                    }
                }
                else if (sourceIndex > dropIndex)
                {
                    if (tileIndex >= dropIndex && tileIndex < sourceIndex)
                    {
                        targetOffset = shift;
                    }
                }

                AnimateRibbonTileOffset(tile, targetOffset);
            }
        }

        private void ClearRibbonTileOffsets()
        {
            if (RibbonTilePanel == null)
            {
                return;
            }

            foreach (var tile in RibbonTilePanel.Children.OfType<Border>())
            {
                AnimateRibbonTileOffset(tile, 0);
            }
        }

        private static void AnimateRibbonTileOffset(Border tile, double offset)
        {
            if (tile.RenderTransform is not TranslateTransform transform)
            {
                transform = new TranslateTransform();
                tile.RenderTransform = transform;
            }

            var animation = new DoubleAnimation
            {
                To = offset,
                Duration = TimeSpan.FromMilliseconds(140),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            transform.BeginAnimation(TranslateTransform.XProperty, animation, HandoffBehavior.SnapshotAndReplace);
        }

        private Border? GetRibbonTile(DependencyObject? origin)
        {
            while (origin != null)
            {
                if (origin is Border border && border.Parent == RibbonTilePanel)
                {
                    return border;
                }

                origin = VisualTreeHelper.GetParent(origin);
            }

            return null;
        }

        private void Token_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _tokenDragStartPoint = e.GetPosition(this);
        }

        private void Token_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isTokenDragInProgress || e.LeftButton != MouseButtonState.Pressed || sender is not FrameworkElement element)
            {
                return;
            }

            var currentPosition = e.GetPosition(this);
            if (Math.Abs(currentPosition.X - _tokenDragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(currentPosition.Y - _tokenDragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            string? tokenValue = element.DataContext switch
            {
                string token => token,
                TokenItem tokenItem => tokenItem.Value,
                _ => null
            };

            if (string.IsNullOrEmpty(tokenValue))
            {
                return;
            }

            bool fromSelected = element.DataContext is TokenItem;
            var payload = new TokenDragPayload(tokenValue, fromSelected);
            var data = new System.Windows.DataObject(TokenDragFormat, payload);

            try
            {
                _isTokenDragInProgress = true;
                System.Windows.DragDrop.DoDragDrop(element, data, System.Windows.DragDropEffects.Move | System.Windows.DragDropEffects.Copy);
            }
            finally
            {
                _isTokenDragInProgress = false;
            }
        }

        private void Token_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(TokenDragFormat) ? System.Windows.DragDropEffects.Move : System.Windows.DragDropEffects.None;
            e.Handled = true;
        }

        private void Token_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;

            if (!e.Data.GetDataPresent(TokenDragFormat) || DataContext is not MainViewModel vm)
            {
                return;
            }

            if (e.Data.GetData(TokenDragFormat) is not TokenDragPayload payload || string.IsNullOrEmpty(payload.Token))
            {
                return;
            }
            int insertIndex = GetTokenInsertIndex(vm, e.OriginalSource as DependencyObject);
            if (insertIndex < 0 || insertIndex > vm.SelectedTokens.Count)
                insertIndex = vm.SelectedTokens.Count;

            // Call ViewModel command to handle insertion/move
            var payloadType = Type.GetType("QuickMediaIngest.ViewModels.TokenInsertPayload, QuickMediaIngest");
            if (payloadType != null)
            {
                var insertPayload = Activator.CreateInstance(payloadType);
                payloadType.GetProperty("Token")?.SetValue(insertPayload, payload.Token);
                payloadType.GetProperty("Index")?.SetValue(insertPayload, insertIndex);
                payloadType.GetProperty("FromSelected")?.SetValue(insertPayload, payload.FromSelected);
                if (vm.InsertTokenCommand.CanExecute(insertPayload))
                    vm.InsertTokenCommand.Execute(insertPayload);
            }
        }

        private static int GetTokenInsertIndex(MainViewModel vm, DependencyObject? origin)
        {
            while (origin != null)
            {
                if (origin is FrameworkElement element && element.DataContext is TokenItem targetToken)
                {
                    int targetIndex = vm.SelectedTokens.IndexOf(targetToken);
                    if (targetIndex >= 0)
                    {
                        return targetIndex;
                    }
                }
                origin = VisualTreeHelper.GetParent(origin);
            }

            return vm.SelectedTokens.Count;
        }

        private void Token_DeleteClick(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            var item = element?.DataContext as TokenItem;
            if (item != null && DataContext is MainViewModel vm)
            {
                if (vm.RemoveTokenCommand.CanExecute(item))
                    vm.RemoveTokenCommand.Execute(item);
            }
        }

        private void TextBox_SelectAll(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                textBox.Dispatcher.BeginInvoke(new Action(textBox.SelectAll), DispatcherPriority.Input);
            }
        }

        private void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not System.Windows.Controls.TextBox textBox)
            {
                return;
            }

            if (!textBox.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                textBox.Focus();
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm && sender is PasswordBox pb)
            {
                // Keep the viewmodel in sync with password box securely
                vm.FtpPass = pb.Password;
            }
        }

        private void PillToggle_Toggled(object sender, RoutedEventArgs e)
        {
            // IsChecked == dark UI (matches App.CurrentIsDarkTheme)
            bool isDark = ThemeToggle?.IsChecked ?? false;

            // Persist and apply theme through the viewmodel so SaveConfig() is invoked
            if (DataContext is MainViewModel vm)
            {
                vm.IsDarkTheme = isDark;
            }
            else
            {
                App.ApplyTheme(!isDark);
            }
        }

        private void Settings_BrowseDestination(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel vm)
                {
                    string initial = vm.DestinationRoot;
                    if (string.IsNullOrWhiteSpace(initial) || !Directory.Exists(initial))
                    {
                        initial = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    }

                    try
                    {
                        var dialog = new Microsoft.Win32.OpenFolderDialog
                        {
                            InitialDirectory = initial,
                            Title = "Select destination folder"
                        };

                        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
                        {
                            vm.DestinationRoot = dialog.FolderName;
                        }
                    }
                    catch
                    {
                        // Fallback for environments where OpenFolderDialog is not available.
                        Process.Start(new ProcessStartInfo("explorer.exe", initial) { UseShellExecute = true });
                        System.Windows.MessageBox.Show("Could not open the folder picker. Explorer was opened instead.", "Select Folder", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch { }
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            // Reuse existing explorer-fallback browse logic
            Settings_BrowseDestination(sender, e);
        }

        private void CloseSettings_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                vm.ShowSettingsDialog = false;
            }
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                if (DataContext is ViewModels.MainViewModel vm && vm.ShowSettingsDialog)
                {
                    vm.ShowSettingsDialog = false;
                    e.Handled = true;
                }
            }
        }

        private void Settings_MoveTokenUp(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.SelectedTokens.Count > 0)
            {
                var lb = FindVisualChild<System.Windows.Controls.ListBox>(this, "");
                // Fallback: move first selected token up; if none selected, do nothing
                var token = vm.SelectedTokens.FirstOrDefault();
                if (token != null)
                {
                    int idx = vm.SelectedTokens.IndexOf(token);
                    if (idx > 0)
                    {
                        vm.SelectedTokens.Move(idx, idx - 1);
                        vm.UpdateNamingFromTokens();
                    }
                }
            }
        }

        private void Settings_MoveTokenDown(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.SelectedTokens.Count > 0)
            {
                var token = vm.SelectedTokens.FirstOrDefault();
                if (token != null)
                {
                    int idx = vm.SelectedTokens.IndexOf(token);
                    if (idx < vm.SelectedTokens.Count - 1)
                    {
                        vm.SelectedTokens.Move(idx, idx + 1);
                        vm.UpdateNamingFromTokens();
                    }
                }
            }
        }

        private void Settings_Save(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SaveConfig();
                vm.ShowSettingsDialog = false;
            }
        }

        private void Settings_Close(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.ShowSettingsDialog = false;
            }
        }

        private void OpenCrashLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string logsDir = Path.Combine(appData, "QuickMediaIngest", "Logs");
                if (!Directory.Exists(logsDir))
                {
                    Directory.CreateDirectory(logsDir);
                }
                Process.Start(new ProcessStartInfo("explorer.exe", logsDir) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to open crash logs folder.");
                System.Windows.MessageBox.Show("Unable to open crash logs folder." + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static T? FindVisualChild<T>(DependencyObject depObj, string name) where T : DependencyObject
        {
            if (depObj == null) return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t)
                {
                    return t;
                }

                var result = FindVisualChild<T>(child, name);
                if (result != null) return result;
            }
            return null;
        }

        private void OpenLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "QuickMediaIngest",
                    "logs");
                if (!Directory.Exists(logPath))
                {
                    Directory.CreateDirectory(logPath);
                }
                Process.Start(new ProcessStartInfo("explorer.exe", logPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to open logs folder.");
            }
        }

        private void ReportBug_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://github.com/edwardlthompson/QuickMediaIngest/issues")
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to open bug report URL.");
            }
        }

        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var method = DataContext?.GetType().GetMethod("SelectAllVisible", Type.EmptyTypes);
            method?.Invoke(DataContext, null);
        }

        private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var method = DataContext?.GetType().GetMethod("DeselectAllVisible", Type.EmptyTypes);
            method?.Invoke(DataContext, null);
        }

        private void DeleteAfterImportCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }
            if (vm.DeleteAfterImportPromptDismissed)
            {
                return;
            }

            var result = MessageBox.Show(
                "Warning: This will permanently remove files from the source after successful copy.",
                "Delete After Import",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);

            vm.DeleteAfterImportPromptDismissed = true;

            if (result != MessageBoxResult.OK && sender is CheckBox checkBox)
            {
                checkBox.IsChecked = false;
            }
        }

        private sealed record TokenDragPayload(string Token, bool FromSelected);
    }
}

// Converter to invert boolean values (True -> False, False -> True)
namespace QuickMediaIngest
{
    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b ? !b : true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b ? !b : true;
        }
    }

    // Converter to show "Import" or "Importing..." based on IsImporting state
    public class BoolToImportingTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool isImporting && isImporting ? "Importing..." : "Import";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}