#nullable enable
using System.ComponentModel;
using System.Runtime.CompilerServices;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.Models
{
    public sealed class SideBySideComparisonState : INotifyPropertyChanged
    {
        private ImportItem? _leftItem;
        private ImportItem? _rightItem;
        private double _zoomFactor = 1.0;
        private bool _isSynchronizedPan = true;

        public ImportItem? LeftItem
        {
            get => _leftItem;
            set { _leftItem = value; OnPropertyChanged(); }
        }

        public ImportItem? RightItem
        {
            get => _rightItem;
            set { _rightItem = value; OnPropertyChanged(); }
        }

        public double ZoomFactor
        {
            get => _zoomFactor;
            set { _zoomFactor = value; OnPropertyChanged(); }
        }

        public bool IsSynchronizedPan
        {
            get => _isSynchronizedPan;
            set { _isSynchronizedPan = value; OnPropertyChanged(); }
        }

        public void Swap()
        {
            var temp = LeftItem;
            LeftItem = RightItem;
            RightItem = temp;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
