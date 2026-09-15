#nullable enable
using Avalonia.Interactivity;
using QuickMediaIngest.Core.AppModel;

namespace QuickMediaIngest.Desktop;

public partial class MainWindow
{
    private async void Queue_Click(object? sender, RoutedEventArgs e) =>
        await IngestBenchQueue.EnqueueAsync(Host, LocCopy);

    private async void Retry_Click(object? sender, RoutedEventArgs e) =>
        await IngestBenchQueue.RetryFailedAsync(Host, LocCopy);

    private async void Resume_Click(object? sender, RoutedEventArgs e) =>
        await IngestBenchQueue.ResumeAsync(Host, LocCopy);
}
