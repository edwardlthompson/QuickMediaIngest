#nullable enable
using Avalonia.Interactivity;
using QuickMediaIngest.Core.AppModel;

namespace QuickMediaIngest.Desktop;

public partial class MainWindow
{
    private void Split_Click(object? sender, RoutedEventArgs e) =>
        IngestBenchWatch.SplitSelected(Host);

    private void Merge_Click(object? sender, RoutedEventArgs e) =>
        IngestBenchWatch.MergeSelected(Host);

    private void Rename_Click(object? sender, RoutedEventArgs e) =>
        IngestBenchWatch.RenameVisible(Host);

    private void DestFolder_LostFocus(object? sender, RoutedEventArgs e)
    {
        IngestBenchNaming.Hydrate(Host.Model);
        IngestBenchNaming.RefreshPreview(Host);
    }
}
