"""GP-15: catalog reports no missing Golden Path slices for this WPF child."""
from __future__ import annotations

import json
from pathlib import Path

from template_gap import feature_gaps

ROOT = Path(__file__).resolve().parents[2]


def main() -> int:
    catalog = json.loads((ROOT / "schemas/golden-path/feature-catalog.json").read_text(encoding="utf-8"))
    gaps = feature_gaps(ROOT, catalog, "dotnet-wpf")
    ids = [g.get("id") for g in gaps]
    assert "about" not in ids, gaps
    assert "crash-capture" not in ids, gaps
    assert "settings" not in ids, gaps
    assert "feedback" not in ids, gaps
    assert "github-feedback" not in ids, gaps
    assert "privacy-report" not in ids, gaps
    assert "display-refresh" not in ids, gaps
    print("OK: WPF catalog detect paths match shipped slices")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
