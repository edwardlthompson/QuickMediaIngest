using System.Windows.Controls;
using System.Windows.Input;
using QuickMediaIngest.ViewModels;

namespace QuickMediaIngest.Controls
{
    public partial class FeedbackOverlayView : UserControl
    {
        public FeedbackOverlayView()
        {
            InitializeComponent();
            KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is not MainViewModel vm || !vm.ShowFeedbackDialog)
            {
                return;
            }

            if (e.Key == Key.Escape)
            {
                vm.DiscardFeedbackCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (vm.FeedbackCanOpenGitHub)
                {
                    vm.OpenFeedbackGitHubCommand.Execute(null);
                }
                else
                {
                    vm.CopyFeedbackCommand.Execute(null);
                }
                e.Handled = true;
            }
        }
    }
}
