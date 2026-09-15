"""GP-19: dirty WPF app paths classify as stack dotnet-wpf, not full/wide."""
from __future__ import annotations

from gate_scope import classify
from local_resources import discover_stacks
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def main() -> int:
    result = classify(["QuickMediaIngest.Core/QuickMediaIngest.Core.csproj"])
    assert result["mode"] == "stacks", result
    assert result["stacks"] == ["dotnet-wpf"], result
    found = discover_stacks(ROOT)
    assert "dotnet-wpf" in found, found
    print("OK: gate_scope + discover_stacks see dotnet-wpf")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
