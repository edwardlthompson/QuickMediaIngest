using System.Windows;
using System.Windows.Controls;

namespace QuickMediaIngest.Controls
{
    public partial class ImportHistoryOverlayView : UserControl
    {
        public ImportHistoryOverlayView()
        {
            InitializeComponent();
        }

        private void CloseImportHistory_Click(object sender, RoutedEventArgs e) =>
            (Window.GetWindow(this) as MainWindow)?.CloseImportHistory_Click(sender, e);
        private void ImportHistory_Clear_Click(object sender, RoutedEventArgs e) =>
            (Window.GetWindow(this) as MainWindow)?.ImportHistory_Clear_Click(sender, e);
    }
}
