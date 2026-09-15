#nullable enable
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Localization;

namespace QuickMediaIngest.Desktop;

public partial class MainWindow
{
    private void ApplyA11yChrome()
    {
        IngestBenchA11y.Apply(Host);
        FlowDirection = Bench.IsRightToLeft
            ? Avalonia.Media.FlowDirection.RightToLeft
            : Avalonia.Media.FlowDirection.LeftToRight;
        if (Application.Current is not null && Bench.HighContrast)
        {
            Application.Current.RequestedThemeVariant = ThemeVariant.Light;
            Background = Brushes.Black;
            Foreground = Brushes.White;
        }

        ShortcutsBody.Text = string.Join(
            "\n",
            AppLocalizer.Get("Toolbar_Import") + " / " + AppLocalizer.Get("Toolbar_Preflight"),
            "F1 — " + AppLocalizer.Get("Help_ShortcutsTitle"),
            "Ctrl+E — " + AppLocalizer.Get("Toolbar_Eject"),
            "Esc — " + AppLocalizer.Get("Btn_Close"));
        IEffect? blur = Bench.OverlayBlurRadius <= 0
            ? null
            : new BlurEffect { Radius = Bench.OverlayBlurRadius };
        foreach (Border border in this.GetVisualDescendants().OfType<Border>())
        {
            if (border.Classes.Contains("overlayScrim"))
            {
                border.Effect = blur;
            }
        }
    }

    private void ShortcutsClose_Click(object? sender, RoutedEventArgs e) =>
        Host.Model.ShowShortcuts = false;
}
