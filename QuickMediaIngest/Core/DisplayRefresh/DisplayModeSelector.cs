#nullable enable
using System.Collections.Generic;

namespace QuickMediaIngest.Core.DisplayRefresh;

public readonly record struct DisplayModeInfo(int Width, int Height, int RefreshHz);

/// <summary>Pick the fastest same-resolution mode. Missing/empty list leaves current unchanged.</summary>
public static class DisplayModeSelector
{
    public static DisplayModeInfo? SelectFastestSameResolution(
        int currentWidth,
        int currentHeight,
        IReadOnlyList<DisplayModeInfo>? modes)
    {
        if (modes is null || modes.Count == 0 || currentWidth <= 0 || currentHeight <= 0)
        {
            return null;
        }

        DisplayModeInfo? best = null;
        foreach (DisplayModeInfo mode in modes)
        {
            if (mode.Width != currentWidth || mode.Height != currentHeight || mode.RefreshHz <= 0)
            {
                continue;
            }

            if (best is null || mode.RefreshHz > best.Value.RefreshHz)
            {
                best = mode;
            }
        }

        return best;
    }
}
