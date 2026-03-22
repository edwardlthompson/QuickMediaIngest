using System.Windows;
using QuickMediaIngest.ViewModels;

namespace QuickMediaIngest
{
    public partial class ImportHistoryWindow : Window
    {
        public ImportHistoryWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(this, "Clear import history? This action cannot be undone.", "Confirm Clear", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    if (DataContext is MainViewModel vm && vm.ClearImportHistoryCommand.CanExecute(null))
                    {
                        vm.ClearImportHistoryCommand.Execute(null);
                    }
                }
            }
            catch { }
        }
    }
}
