"""UX-B3: command bar docks destination into View overflow under 1100px."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
APP = ROOT / "QuickMediaIngest"


def main() -> int:
    core = (APP / "Core/Chrome/CommandBarOverflow.cs").read_text(encoding="utf-8")
    assert "CompactWidthPx = 1100" in core, core
    xaml = (APP / "MainWindow.xaml").read_text(encoding="utf-8")
    assert "ViewOverflowHeader" in xaml, xaml
    assert "ShowDestinationChipInPrimaryBar" in xaml, xaml
    assert "CommandBarCompact" in xaml, xaml
    desk = (ROOT / "QuickMediaIngest.Desktop/MainWindow.axaml").read_text(encoding="utf-8")
    assert "CommandBarCompact" in desk, desk
    assert 'Name="DestChip"' in desk and "ShowPrimaryExtras" not in desk, desk
    assert "ShowOverflow" in desk, desk
    print("OK: command bar overflow at 1100px (View …; Save location stays in the primary bar)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
