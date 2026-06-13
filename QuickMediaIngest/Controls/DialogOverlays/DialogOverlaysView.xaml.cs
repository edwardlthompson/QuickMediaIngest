using System.Windows;
using System.Windows.Controls;

namespace QuickMediaIngest.Controls
{
    public partial class DialogOverlaysView : UserControl
    {
        public DialogOverlaysView()
        {
            InitializeComponent();
        }

        public Grid FtpOverlay => AddFtpOverlay;

        private void AddFtpOverlay_Loaded(object sender, RoutedEventArgs e) =>
            (Window.GetWindow(this) as MainWindow)?.AddFtpOverlay_Loaded(sender, e);
        private void AddFtpOverlay_Unloaded(object sender, RoutedEventArgs e) =>
            (Window.GetWindow(this) as MainWindow)?.AddFtpOverlay_Unloaded(sender, e);
        private void OpenLogs_Click(object sender, RoutedEventArgs e) =>
            (Window.GetWindow(this) as MainWindow)?.OpenLogs_Click(sender, e);
        private void ReportBug_Click(object sender, RoutedEventArgs e) =>
            (Window.GetWindow(this) as MainWindow)?.ReportBug_Click(sender, e);
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) =>
            (Window.GetWindow(this) as MainWindow)?.PasswordBox_PasswordChanged(sender, e);
    }
}
