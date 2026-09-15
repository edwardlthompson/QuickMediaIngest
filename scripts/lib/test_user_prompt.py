"""UX-M7: non-destructive prompts go through IUserPrompt; destructive MessageBoxes stay."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
APP = ROOT / "QuickMediaIngest"


def main() -> int:
    iface = (APP / "Core/Prompt/IUserPrompt.cs").read_text(encoding="utf-8")
    assert "Task NotifyAsync" in iface, iface
    assert "Task<bool> ConfirmAsync" in iface, iface
    silent = (APP / "Core/Prompt/SilentUserPrompt.cs").read_text(encoding="utf-8")
    assert "Task.FromResult(true)" in silent, silent
    overlay = (APP / "Controls/DialogOverlays/DialogOverlaysView.xaml").read_text(encoding="utf-8")
    assert "ShowUserPromptDialog" in overlay, "prompt overlay missing"
    engine = (APP / "ViewModels/MainViewModel.ImportEngine.partial.cs").read_text(encoding="utf-8")
    assert "MessageBox" not in engine, "ImportEngine still uses MessageBox"
    assert "_userPrompt" in engine, engine
    delete_helper = (APP / "Services/DeleteAfterImportConfirmHelper.cs").read_text(encoding="utf-8")
    assert "MessageBox" in delete_helper, "delete-after must keep a destructive confirm"
    cancel = (APP / "ViewModels/MainViewModel.Import.partial.cs").read_text(encoding="utf-8")
    assert "MessageBox" in cancel, "cancel-active-import must keep a destructive confirm"
    history = (APP / "ViewModels/MainViewModel.FtpCollections.partial.cs").read_text(encoding="utf-8")
    assert "MessageBox" in history, "clear-history must keep a destructive confirm"
    print("OK: IUserPrompt overlay for non-destructive prompts; destructive MessageBoxes kept")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
