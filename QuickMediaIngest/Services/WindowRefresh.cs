#nullable enable
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using QuickMediaIngest.Core.DisplayRefresh;

namespace QuickMediaIngest.Services;

/// <summary>WPF port of display-refresh: vote HIGH frame rate on scroll surfaces. Does not change OS display mode.</summary>
public static class WindowRefresh
{
    public static void TryApply(Window window)
    {
        if (window is null)
        {
            return;
        }

        try
        {
            int width = (int)Math.Max(1, window.ActualWidth);
            int height = (int)Math.Max(1, window.ActualHeight);
            var modes = new[]
            {
                new DisplayModeInfo(width, height, 60),
                new DisplayModeInfo(width, height, 120)
            };
            DisplayModeInfo? best = DisplayModeSelector.SelectFastestSameResolution(width, height, modes);
            int hz = best?.RefreshHz ?? 60;
            ApplyHighRefreshScroll(window, hz);
        }
        catch
        {
            // Missing display metadata leaves the window unchanged.
        }
    }

    public static void ApplyHighRefreshScroll(DependencyObject root, int refreshHz)
    {
        int rate = Math.Clamp(refreshHz, 30, 240);
        ApplyToScrollViewers(root, rate);
    }

    private static void ApplyToScrollViewers(DependencyObject current, int rate)
    {
        if (current is ScrollViewer)
        {
            Timeline.SetDesiredFrameRate(current, rate);
        }

        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(current);
        for (int i = 0; i < count; i++)
        {
            ApplyToScrollViewers(System.Windows.Media.VisualTreeHelper.GetChild(current, i), rate);
        }
    }
}
