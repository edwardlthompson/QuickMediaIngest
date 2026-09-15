"""Web probe for sprint-smoke. This child is WPF; skip the examples/web HTTP check."""
from __future__ import annotations

from pathlib import Path

from sprint_smoke_probes import ProbeResult


def probe_web(root: Path, budget: dict[str, int]) -> ProbeResult:
    _ = budget
    marker = root / "examples" / "web" / "package.json"
    if not marker.is_file():
        return ProbeResult("web", True, "skip: no examples/web (dotnet-wpf child)", skipped=True)
    return ProbeResult("web", True, "examples/web present; HTTP probe not required for WPF child", skipped=True)
