using System.Windows.Controls;
using System.Windows.Input;
using QuickMediaIngest.Services;
using QuickMediaIngest.ViewModels;

namespace QuickMediaIngest.Controls
{
    public partial class PreferencesOverlayView : UserControl
    {
        private bool _deleteAfterImportUserInitiated;

        public PreferencesOverlayView()
        {
            InitializeComponent();
        }

        private void DeleteAfterImportCheckBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _deleteAfterImportUserInitiated = true;
        }

        private void DeleteAfterImportCheckBox_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            DeleteAfterImportConfirmHelper.HandleChecked(
                vm,
                ref _deleteAfterImportUserInitiated,
                () =>
                {
                    if (sender is CheckBox cb)
                    {
                        cb.IsChecked = false;
                    }
                });
        }
    }
}
