#nullable enable
using System;

namespace QuickMediaIngest.Core.ImportUi
{
    /// <summary>Hedge-style import scene: clamp percents and decide when Open destination is shown.</summary>
    public static class ImportProgressScene
    {
        public static int ClampPercent(int value) => Math.Clamp(value, 0, 100);

        public static bool ShowOpenDestination(bool isImporting, string? destinationRoot)
            => !isImporting && !string.IsNullOrWhiteSpace(destinationRoot);

        public static bool DimList(bool isImporting) => false;
    }
}
