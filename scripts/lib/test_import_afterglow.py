"""UX-D8: import afterglow banner uses Core folder label + Open folder command."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
APP = ROOT / "QuickMediaIngest"


def main() -> int:
    core = (APP / "Core/ImportUi/ImportAfterglow.cs").read_text(encoding="utf-8")
    assert "FolderLabel" in core, core
    xaml = (APP / "MainWindow.xaml").read_text(encoding="utf-8")
    assert "ShowImportAfterglow" in xaml, "afterglow banner missing"
    assert "OpenImportAfterglowFolderCommand" in xaml, xaml
    assert "DestinationChipLabel" in xaml, "destination chip missing"
    engine = (APP / "ViewModels/MainViewModel.ImportEngine.partial.cs").read_text(encoding="utf-8")
    assert "ShowImportAfterglowBanner" in engine, engine
    assert "ShowWindowsImportCompletionNotificationAsync" not in engine, "completion still modal"
    print("OK: import afterglow banner + Open folder")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
