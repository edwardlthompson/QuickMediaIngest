"""GP-18: shipped feature specs map to QuickMediaIngest/, not examples/web."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
FEATURES = ROOT / "docs/features"
SKIP = {"_template.md", "_handoff.md"}


def main() -> int:
    bad: list[str] = []
    for path in sorted(FEATURES.glob("*.md")):
        if path.name in SKIP:
            continue
        text = path.read_text(encoding="utf-8")
        start = text.find("## Container map")
        if start < 0:
            continue
        blob = text[start:]
        if "examples/web" in blob or "examples/android" in blob:
            bad.append(path.name)
    assert not bad, bad
    print("OK: feature container maps use QuickMediaIngest/ (not examples/web|android)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
