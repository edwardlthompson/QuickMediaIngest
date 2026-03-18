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
    }
}
