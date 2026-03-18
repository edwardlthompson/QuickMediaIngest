using System.Windows;
using System.Windows.Input;
using QuickMediaIngest.ViewModels;

namespace QuickMediaIngest
{
    public partial class MainWindow : Window
    {
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
            private void Token_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && sender is FrameworkElement element)
            {
                var token = element.DataContext as string;
                if (!string.IsNullOrEmpty(token))
                {
                    System.Windows.DragDrop.DoDragDrop(element, token, System.Windows.DragDropEffects.Copy);
                }
            }
        }

        private void Token_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            e.Effects = System.Windows.DragDropEffects.Copy;
            e.Handled = true;
        }

        private void Token_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(string)) && DataContext is MainViewModel vm)
            {
                var token = e.Data.GetData(typeof(string)) as string;
                if (!string.IsNullOrEmpty(token))
                {
                    vm.SelectedTokens.Add(new TokenItem { Value = token });
                    vm.UpdateNamingFromTokens();
                }
            }
        }

        private void Token_DeleteClick(object sender, System.Windows.RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            var item = element?.DataContext as TokenItem;
            if (item != null && DataContext is MainViewModel vm)
            {
                vm.SelectedTokens.Remove(item);
                vm.UpdateNamingFromTokens();
            }
        }
    }
}