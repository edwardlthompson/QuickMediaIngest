#nullable enable
using Avalonia.Interactivity;
using QuickMediaIngest.Core.AppModel;

namespace QuickMediaIngest.Desktop;

public partial class MainWindow
{
    private async void Eject_Click(object? sender, RoutedEventArgs e) =>
        await IngestBenchEject.EjectNowAsync(Host, groups: null, LocCopy);
}
