#nullable enable
using Avalonia.Controls;
using Avalonia.Interactivity;
using QuickMediaIngest.Core.AppModel;

namespace QuickMediaIngest.Desktop;

public partial class MainWindow
{
    private void Pick_Click(object? sender, RoutedEventArgs e) =>
        IngestBenchCull.PickSelected(Host);

    private void Reject_Click(object? sender, RoutedEventArgs e) =>
        IngestBenchCull.RejectSelected(Host);

    private void Rating_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (RatingBox?.SelectedItem is int rating)
        {
            IngestBenchCull.SetRating(Host, rating);
        }
    }

    private void Color_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (ColorBox?.SelectedItem is string color)
        {
            IngestBenchCull.SetColor(Host, color);
        }
    }
}
