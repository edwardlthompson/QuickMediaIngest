#!/usr/bin/env bash
# Triage OpenSSF Scorecard SARIF when present; informational pass when no artifact yet.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SARIF="${1:-$ROOT/scorecard-results.sarif}"
MIN_SCORE="${SCORECARD_MIN_SCORE:-6.0}"

if [ ! -f "$SARIF" ]; then
  echo "No Scorecard SARIF at $SARIF — skip (run scorecard workflow or download artifact)"
  exit 0
fi

python3 - "$SARIF" "$MIN_SCORE" << 'PY'
import json, sys
path, min_score = sys.argv[1], float(sys.argv[2])
with open(path, encoding="utf-8") as f:
    data = json.load(f)

runs = data.get("runs") or []
if not runs:
    print("WARN: empty SARIF — review Scorecard workflow")
    sys.exit(0)

rules = {r["id"]: r for r in runs[0].get("tool", {}).get("driver", {}).get("rules", [])}
critical = []
for result in runs[0].get("results", []):
    rule = rules.get(result.get("ruleId", ""), {})
    level = (rule.get("defaultConfiguration") or {}).get("level") or result.get("level") or "note"
    if level in ("error", "warning"):
        critical.append((result.get("ruleId"), level, (result.get("message") or {}).get("text", "")))

if critical:
    print(f"Scorecard findings to triage ({len(critical)}):")
    for rule_id, level, msg in critical[:20]:
        print(f"  [{level}] {rule_id}: {msg[:120]}")
    if len(critical) > 20:
        print(f"  ... and {len(critical) - 20} more")
    print("Add BUILD_PLAN [AGENT] rows for unresolved items.")
    sys.exit(1)

print("Scorecard SARIF triage OK (no error/warning findings)")
PY
