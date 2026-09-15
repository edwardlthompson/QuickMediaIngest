"""UX-B2: import scene offers Open destination when idle; list stays undimmed."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
APP = ROOT / "QuickMediaIngest"


def main() -> int:
    core = (APP / "Core/ImportUi/ImportProgressScene.cs").read_text(encoding="utf-8")
    assert "ShowOpenDestination" in core, core
    assert "DimList" in core, core
    overlay = (APP / "Controls/DialogOverlays/DialogOverlaysView.xaml").read_text(encoding="utf-8")
    assert "ImportScenePercentText" in overlay, "big percent missing"
    assert "OpenImportAfterglowFolderCommand" in overlay, "Open destination missing on scene"
    assert "DismissImportProgressSceneCommand" in overlay, overlay
    main = (APP / "MainWindow.xaml").read_text(encoding="utf-8")
    assert "DimShootListForImport" in main, "list dim trigger missing"
    engine = (APP / "ViewModels/MainViewModel.ImportEngine.partial.cs").read_text(encoding="utf-8")
    assert "keepImportScene" in engine, engine
    print("OK: import scene + Open destination on done; list stays undimmed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
