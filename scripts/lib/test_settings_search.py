"""UX-M4b: Preferences search filters sections without WPF."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
APP = ROOT / "QuickMediaIngest"


def main() -> int:
    src = (APP / "Core/Settings/SettingsSearch.cs").read_text(encoding="utf-8")
    assert "IndexOf(needle, StringComparison.CurrentCultureIgnoreCase)" in src, src
    xaml = (APP / "Controls/PreferencesOverlay/PreferencesOverlayView.xaml").read_text(encoding="utf-8")
    assert "SettingsSearchQuery" in xaml, "search box missing"
    assert "SettingsSearchNoResults" in xaml, "empty results missing"
    vm = (APP / "ViewModels/MainViewModel.SettingsSearch.partial.cs").read_text(encoding="utf-8")
    assert "SettingsSectionAppearanceVisible" in vm, vm
    print("OK: Preferences search-in-settings (SettingsSearch + overlay box)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
