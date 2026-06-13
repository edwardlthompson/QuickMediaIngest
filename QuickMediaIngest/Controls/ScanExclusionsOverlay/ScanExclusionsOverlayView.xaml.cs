using System.Windows;
using System.Windows.Controls;

namespace QuickMediaIngest.Controls
{
    public partial class ScanExclusionsOverlayView : UserControl
    {
        public ScanExclusionsOverlayView()
        {
            InitializeComponent();
        }

        private void CloseScanExclusions_Click(object sender, RoutedEventArgs e) =>
            (Window.GetWindow(this) as MainWindow)?.CloseScanExclusions_Click(sender, e);
    }
}
