"""UX-AAA: Import primary control is at least 44×44 (WCAG 2.5.5)."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
APP = ROOT / "QuickMediaIngest"


def main() -> int:
    app = (APP / "App.xaml").read_text(encoding="utf-8")
    style_start = app.find('x:Key="DialogPrimaryButtonStyle"')
    assert style_start >= 0, app
    style = app[style_start : style_start + 500]
    assert 'MinHeight" Value="44"' in style, style
    assert 'MinWidth" Value="88"' in style, style
    main = (APP / "MainWindow.xaml").read_text(encoding="utf-8")
    assert 'Command="{Binding ImportCommand}" Style="{StaticResource DialogPrimaryButtonStyle}"' in main, main
    desk = (ROOT / "QuickMediaIngest.Desktop/MainWindow.axaml").read_text(encoding="utf-8")
    assert 'Name="ImportButton"' in desk and 'MinHeight="44"' in desk and 'MinWidth="88"' in desk, desk
    print("OK: Import hit target 44×44 via DialogPrimaryButtonStyle")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
