#nullable enable
using Avalonia.Controls;
using Avalonia.Interactivity;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Desktop;

public partial class MainWindow
{
    private void ShootExpand_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ItemGroup group })
        {
            IngestBenchShoots.ToggleExpanded(group);
        }
    }

    private void ShootIgnore_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ItemGroup group })
        {
            IngestBenchShoots.IgnoreFolder(Host, group);
        }
    }

    private void ExpandAll_Click(object? sender, RoutedEventArgs e)
    {
        Bench.AllGroupsExpanded = !Bench.AllGroupsExpanded;
        IngestBenchShoots.SetAllExpanded(Host, Bench.AllGroupsExpanded);
    }

    private void Hours_Changed(object? sender, RoutedEventArgs e)
    {
        Host.Persist();
        Host.RefreshScan();
    }

    private void Thumb_Changed(object? sender, RoutedEventArgs e) => Host.Persist();

    private void FileType_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (!_viewReady)
        {
            return;
        }

        IngestBenchShoots.ApplyFilter(Host);
        Host.Persist();
    }

    private void FilterChip_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id })
        {
            IngestBenchFilters.RemoveChip(Host, id);
        }
    }
}
