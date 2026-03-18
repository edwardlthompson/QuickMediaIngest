using System.Windows;

namespace QuickMediaIngest
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new QuickMediaIngest.ViewModels.MainViewModel();
        }
    }
}
