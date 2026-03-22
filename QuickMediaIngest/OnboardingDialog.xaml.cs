using System.Windows;

namespace QuickMediaIngest
{
    public partial class OnboardingDialog : Window
    {
        public OnboardingDialog()
        {
            InitializeComponent();
        }

        private void GotIt_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
