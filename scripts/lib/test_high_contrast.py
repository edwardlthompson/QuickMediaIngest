"""UX-HC: high-contrast tokens remap to SystemColors and skip overlay blur."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
APP = ROOT / "QuickMediaIngest"


def main() -> int:
    core = (APP / "Core/Chrome/HighContrastTokens.cs").read_text(encoding="utf-8")
    assert "OverlayBlurRadius" in core, core
    theme = (APP / "App.Theme.partial.cs").read_text(encoding="utf-8")
    assert "ApplyHighContrastTokensIfNeeded" in theme, theme
    assert "SystemColors.WindowTextColor" in theme, theme
    assert "SystemParameters.HighContrast" in theme, theme
    print("OK: high-contrast token wiring (SystemColors + no overlay blur)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
