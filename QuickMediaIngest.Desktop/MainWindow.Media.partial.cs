#nullable enable
using System.IO;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using QuickMediaIngest.Core.AppModel;

namespace QuickMediaIngest.Desktop;

public partial class MainWindow
{
    private void Compare_Click(object? sender, RoutedEventArgs e)
    {
        IngestBenchThumbs.CompareSelected(Host);
        LoadCompare();
    }

    private void CompareClose_Click(object? sender, RoutedEventArgs e) =>
        Host.Model.ShowCompare = false;

    private void Purge_Click(object? sender, RoutedEventArgs e)
    {
        IngestBenchThumbs.Purge(Host);
        SetPreviewBitmap(null);
        CompareLeft.Source = null;
        CompareRight.Source = null;
    }

    private void LoadCompare()
    {
        CompareLeft.Source = TryBitmap(Host.Model.CompareLeftPath);
        CompareRight.Source = TryBitmap(Host.Model.CompareRightPath);
    }

    private static Bitmap? TryBitmap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        return new Bitmap(path);
    }
}
