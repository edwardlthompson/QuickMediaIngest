using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media.Animation;
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
        }

        public void ApplyWindowStateFromViewModel()
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            Width = vm.SavedWindowWidth;
            Height = vm.SavedWindowHeight;
            if (vm.SavedWindowMaximized)
            {
                WindowState = WindowState.Maximized;
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
                    vmSize.SaveWindowState(Width, Height, false);

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
                    vm.SaveWindowState(Width, Height, false);
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

            var data = new DataObject(RibbonTileDragFormat, tile);

            try
            {
                _isRibbonTileDragInProgress = true;
                _activeRibbonDraggedTile = tile;
                DragDrop.DoDragDrop(tile, data, DragDropEffects.Move);
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
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            e.Effects = DragDropEffects.Move;

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
            var data = new DataObject(TokenDragFormat, payload);

            try
            {
                _isTokenDragInProgress = true;
                DragDrop.DoDragDrop(element, data, DragDropEffects.Move | DragDropEffects.Copy);
            }
            finally
            {
                _isTokenDragInProgress = false;
            }
        }

        private void Token_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(TokenDragFormat) ? DragDropEffects.Move : DragDropEffects.None;
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
            {
                insertIndex = vm.SelectedTokens.Count;
            }

            if (payload.FromSelected)
            {
                int existingIndex = vm.SelectedTokens
                    .Select((item, index) => new { item, index })
                    .FirstOrDefault(x => x.item.Value == payload.Token)?.index ?? -1;

                if (existingIndex >= 0)
                {
                    var movingItem = vm.SelectedTokens[existingIndex];
                    vm.SelectedTokens.RemoveAt(existingIndex);
                    if (existingIndex < insertIndex) insertIndex--;
                    if (insertIndex < 0) insertIndex = 0;
                    if (insertIndex > vm.SelectedTokens.Count) insertIndex = vm.SelectedTokens.Count;
                    vm.SelectedTokens.Insert(insertIndex, movingItem);
                    vm.UpdateNamingFromTokens();
                }
                return;
            }

            // Token placeholders are single-use and should not be duplicated.
            if (payload.Token.StartsWith("[") && payload.Token.EndsWith("]") &&
                vm.SelectedTokens.Any(t => t.Value == payload.Token))
            {
                return;
            }

            vm.SelectedTokens.Insert(insertIndex, new TokenItem { Value = payload.Token });
            vm.UpdateNamingFromTokens();

            if (payload.Token.StartsWith("[") && payload.Token.EndsWith("]"))
            {
                vm.AvailableTokens.Remove(payload.Token);
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
                vm.SelectedTokens.Remove(item);
                vm.UpdateNamingFromTokens();

                if (item.Value.StartsWith("[") && item.Value.EndsWith("]") && !vm.AvailableTokens.Contains(item.Value))
                {
                    vm.AvailableTokens.Add(item.Value);
                }
            }
        }

        private void TextBox_SelectAll(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.Dispatcher.BeginInvoke(new Action(textBox.SelectAll), DispatcherPriority.Input);
            }
        }

        private void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                return;
            }

            if (!textBox.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                textBox.Focus();
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