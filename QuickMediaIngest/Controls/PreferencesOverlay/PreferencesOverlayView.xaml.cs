using System.Windows;
using System.Windows.Controls;

namespace QuickMediaIngest.Controls
{
    public partial class PreferencesOverlayView : UserControl
    {
        public PreferencesOverlayView()
        {
            InitializeComponent();
        }

        private void CloseSettings_Click(object sender, RoutedEventArgs e) =>
            (Window.GetWindow(this) as MainWindow)?.CloseSettings_Click(sender, e);
        private void Browse_Click(object sender, RoutedEventArgs e) =>
            (Window.GetWindow(this) as MainWindow)?.Browse_Click(sender, e);
    }
}
