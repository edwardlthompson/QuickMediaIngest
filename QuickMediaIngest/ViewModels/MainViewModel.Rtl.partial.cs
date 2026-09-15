#nullable enable
using System.Windows;
using QuickMediaIngest.Core.Chrome;

namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel
    {
        public FlowDirection WindowFlowDirection => UiReadingOrder.IsRightToLeft(UiLanguage)
            ? FlowDirection.RightToLeft
            : FlowDirection.LeftToRight;

        private void RefreshWindowFlowDirection() => OnPropertyChanged(nameof(WindowFlowDirection));
    }
}
