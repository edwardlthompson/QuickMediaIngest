"""UX-RTL: MainWindow FlowDirection follows culture TextInfo.IsRightToLeft."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
APP = ROOT / "QuickMediaIngest"


def main() -> int:
    core = (APP / "Core/Chrome/UiReadingOrder.cs").read_text(encoding="utf-8")
    assert "IsRightToLeft" in core, core
    assert "CurrentUICulture" in core, core
    xaml = (APP / "MainWindow.xaml").read_text(encoding="utf-8")
    assert 'FlowDirection="{Binding WindowFlowDirection}"' in xaml, xaml
    vm = (APP / "ViewModels/MainViewModel.Rtl.partial.cs").read_text(encoding="utf-8")
    assert "WindowFlowDirection" in vm, vm
    ftp = (APP / "ViewModels/MainViewModel.Ftp.partial.cs").read_text(encoding="utf-8")
    assert "RefreshWindowFlowDirection" in ftp, ftp
    print("OK: FlowDirection bound for RTL locales")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
