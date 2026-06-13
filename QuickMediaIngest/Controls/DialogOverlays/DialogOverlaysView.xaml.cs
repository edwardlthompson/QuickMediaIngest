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
    }
}
