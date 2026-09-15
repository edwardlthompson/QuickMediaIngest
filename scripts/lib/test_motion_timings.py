"""UX-M6: motion tokens skip to zero when reduced motion is on."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "QuickMediaIngest/Core/Motion/MotionTimings.cs"


def main() -> int:
    text = SRC.read_text(encoding="utf-8")
    for needle in (
        "OverlayEnterMs = 180",
        "ChevronMs = 120",
        "SidebarExpandedPx = 260",
        "SidebarCollapsedPx = 64",
        "return TimeSpan.Zero",
    ):
        assert needle in text, needle
    print("OK: MotionTimings overlay 180 / chevron 120 / sidebar 260→64 / reduced-motion zero")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
