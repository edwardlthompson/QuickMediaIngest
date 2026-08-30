"""I-65: child product board rows must be visible to build-sprint-status."""
from __future__ import annotations

from build_sprint import _child_product_status, parse_child_product_rows

SAMPLE = """
## Sequential lane

✅ `[AGENT]` already done
🔲 `[HUMAN]` Live OP13 smoke: PreferAdb browse

## Golden Path catch-up (named 1–8)

- ✅ [AGENT] /feature GP-1: already done
- 🔲 [AUTO] feature-gate.sh --stack dotnet-wpf
- 🔲 [HUMAN] Smoke About donate / Check now after align

## Ongoing Maintenance

- 🔲 [AGENT] /feature I-02: LibRaw/libvips-first decode path

## Human & device (after automation)

- 🔲 [HUMAN] WPF UI sign-off via scripts/run-human-signoffs.ps1
"""


def main() -> int:
    rows = parse_child_product_rows(SAMPLE)
    owners = [(r.sprint, r.owner, r.task.split(":")[0][:40]) for r in rows]
    assert any(r.owner == "HUMAN" and r.sprint == "Sequential lane" for r in rows), owners
    assert any(r.owner == "AUTO" and r.sprint == "Golden Path catch-up" for r in rows), owners
    assert any(r.owner == "AGENT" and "I-02" in r.task for r in rows), owners
    backlog = {
        "Golden Path catch-up|Smoke About donate / Check now after align",
        "Human & device|WPF UI sign-off via scripts/run-human-signoffs.ps1",
    }
    status = _child_product_status(SAMPLE, backlog)
    assert status is not None
    assert status["next_row"]["owner"] == "AUTO"
    assert "feature-gate" in status["next_row"]["task"]
    assert status["open_agent_auto"] == 2
    print("ok", status["next_row"]["owner"], status["next_row"]["task"][:48])
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
