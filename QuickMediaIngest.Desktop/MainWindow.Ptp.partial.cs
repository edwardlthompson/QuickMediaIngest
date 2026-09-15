#nullable enable
using Avalonia.Interactivity;
using QuickMediaIngest.Core.AppModel;

namespace QuickMediaIngest.Desktop;

public partial class MainWindow
{
    private void Ptp_Click(object? sender, RoutedEventArgs e) => IngestBenchPtp.Open(Host);

    private void PtpScan_Click(object? sender, RoutedEventArgs e) => IngestBenchPtp.Confirm(Host);

    private void PtpCancel_Click(object? sender, RoutedEventArgs e) => IngestBenchPtp.Close(Host);
}
