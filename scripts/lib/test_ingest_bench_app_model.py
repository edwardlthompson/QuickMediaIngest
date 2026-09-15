"""LX-L1b: IngestBench AppModel is the shared first-run / empty / import flags type."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
APP = ROOT / "QuickMediaIngest"


def main() -> int:
    model = (APP / "Core/AppModel/IngestBenchAppModel.cs").read_text(encoding="utf-8")
    assert "isFirstRun" in model and "deleteAfterImport" in model, model
    assert "isImporting" in model and "ShowWaitingForCard" in model, model
    assert "commandBarCompact" in model and "ShowPrimaryExtras" in model, model
    assert "Notifications" in model, model
    vm = (APP / "ViewModels/MainViewModel.AppModel.partial.cs").read_text(encoding="utf-8")
    assert "IngestBenchAppModel Bench" in vm, vm
    assert "SyncIngestBench" in vm, vm
    assert "IsFirstRun" in vm, vm
    print("OK: IngestBench AppModel + MainViewModel forwarding")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
