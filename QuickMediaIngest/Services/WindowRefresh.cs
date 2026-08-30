#nullable enable
using System;
using System.Windows;
using QuickMediaIngest.Core.DisplayRefresh;

namespace QuickMediaIngest.Services;

/// <summary>
/// WPF port of display-refresh: pick the fastest same-size mode. Does not call
/// ChangeDisplaySettings. Timeline.SetDesiredFrameRate requires a Timeline, not a ScrollViewer.
/// </summary>
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
            _ = DisplayModeSelector.SelectFastestSameResolution(width, height, modes);
        }
        catch
        {
            // Missing display metadata leaves the window unchanged.
        }
    }
}
