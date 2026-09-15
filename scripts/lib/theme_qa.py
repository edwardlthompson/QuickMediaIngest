"""THEME_QA without a Windows glance: tokens, hit targets, first-run, command bar."""
from __future__ import annotations

from pathlib import Path

ACCENT = "#007ACC"
FORBIDDEN_PRIMARY = ('PrimaryColor="Yellow"', "PrimaryColor='Yellow'")


def check_repo(root: Path) -> list[str]:
    errors: list[str] = []
    colors = (root / "QuickMediaIngest/Themes/Colors.xaml").read_text(encoding="utf-8")
    for key in ("DarkTheme.Accent", "LightTheme.Accent", "Theme.Accent"):
        needle = f'x:Key="{key}">{ACCENT}<'
        if needle not in colors:
            errors.append(f"Colors.xaml {key} must be {ACCENT}")
    app = (root / "QuickMediaIngest/App.xaml").read_text(encoding="utf-8")
    if f'Color="{ACCENT}"' not in app or "AppAccentBrush" not in app:
        errors.append(f"App.xaml AppAccentBrush must be {ACCENT}")
    if 'PrimaryColor="Blue"' not in app:
        errors.append("App.xaml BundledTheme PrimaryColor must be Blue")
    if any(token in app for token in FORBIDDEN_PRIMARY):
        errors.append("App.xaml must not use Material Yellow as PrimaryColor")
    if 'x:Key="DialogPrimaryButtonStyle"' not in app or 'Value="44"' not in app:
        errors.append("DialogPrimaryButtonStyle must set MinHeight 44")
    typo = (root / "QuickMediaIngest/Themes/Typography.xaml").read_text(encoding="utf-8")
    if 'x:Key="Theme.BodyTextBlock"' not in typo or 'FontSize" Value="14"' not in typo:
        errors.append("Theme.BodyTextBlock must be 14px")
    main = (root / "QuickMediaIngest/MainWindow.xaml").read_text(encoding="utf-8")
    if 'TextElement.FontSize="14"' not in main:
        errors.append("MainWindow body TextElement.FontSize must be 14")
    if 'MinWidth="24"' not in main or 'MinHeight="24"' not in main:
        errors.append("filter-chip remove must be at least 24×24")
    if "Toolbar_Import" not in main or "Toolbar_Preflight" not in main or "Toolbar_Refresh" not in main:
        errors.append("command bar must keep Import, Dry run, and Refresh")
    code = (root / "QuickMediaIngest/MainWindow.xaml.cs").read_text(encoding="utf-8")
    if "QMI_SKIP_ONBOARDING" not in code or "ShowOnboarding" not in code:
        errors.append("first-run onboarding (ShowOnboarding / QMI_SKIP_ONBOARDING) missing")
    if not (root / "QuickMediaIngest/OnboardingDialog.xaml").is_file():
        errors.append("OnboardingDialog.xaml missing")
    desk = root / "QuickMediaIngest.Desktop/MainWindow.axaml"
    if desk.is_file():
        axaml = desk.read_text(encoding="utf-8")
        if 'FontSize="14"' not in axaml:
            errors.append("Desktop MainWindow FontSize must be 14")
        if ACCENT not in axaml:
            errors.append(f"Desktop Import accent must be {ACCENT}")
        if 'MinHeight="44"' not in axaml or 'MinWidth="88"' not in axaml:
            errors.append("Desktop Import must be at least 44×88")
        if "CanImport" not in axaml or "Onboarding_Body" not in (root / "QuickMediaIngest.Desktop/MainWindow.axaml.cs").read_text(encoding="utf-8"):
            errors.append("Desktop first-run must bind onboarding body and disable Import until a shoot is selected")
        if "SidebarPrefs" not in axaml or 'Content="PTP"' not in axaml.split("ShowOverflow", 1)[-1]:
            errors.append("Desktop first paint must keep PTP in View overflow and Settings always visible")
    return errors


def main() -> int:
    errors = check_repo(Path.cwd())
    if errors:
        print("THEME_QA check failed:")
        for item in errors:
            print(f"  {item}")
        return 1
    print("OK: THEME_QA tokens, hit targets, first-run, and command bar")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
