using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Threading;
using QuickMediaIngest.ViewModels;

namespace QuickMediaIngest
{
    public partial class MainWindow : Window
    {
        private const string TokenDragFormat = "QuickMediaIngest.TokenPayload";
        private Point _tokenDragStartPoint;
        private bool _isTokenDragInProgress;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel();
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