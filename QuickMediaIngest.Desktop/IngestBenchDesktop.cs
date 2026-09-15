#nullable enable
using Avalonia.Controls;
using QuickMediaIngest.Core.AppModel;

namespace QuickMediaIngest.Desktop;

internal static class IngestBenchDesktop
{
    public static (MainWindow Window, IngestBenchHost Host) CreateMainWindow(string[]? args = null)
    {
        IngestBenchHost host = IngestBenchHost.Create(new AvaloniaUiDispatcher());
        return (new MainWindow(host, LaunchMediaRoots.Parse(args)), host);
    }
}
