#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;

namespace QuickMediaIngest.Core.AppModel
{
    public sealed partial class VolumeChoice : ObservableObject
    {
        [ObservableProperty] private bool isSelected = true;

        public VolumeChoice(string path, string label, string kind)
        {
            Path = path;
            Label = label;
            Kind = kind;
        }

        public string Path { get; }

        public string Label { get; }

        public string Kind { get; }
    }
}
