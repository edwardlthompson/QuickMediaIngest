#!/usr/bin/env bash
# Count open Critical/High Dependabot alerts (paginated).
# Usage: scripts/count-critical-high-dependabot.sh
# Exit 0 prints count to stdout; exit 1 on API/auth error.
set -euo pipefail

if ! command -v gh >/dev/null 2>&1; then
  echo "ERROR: gh CLI required" >&2
  exit 1
fi

REPO="${GITHUB_REPOSITORY:-${GITHUB_REPO:-$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null || true)}}"
if [ -z "$REPO" ]; then
  echo "ERROR: gh auth required" >&2
  exit 1
fi

if command -v python3 >/dev/null 2>&1; then PY=python3
elif command -v python >/dev/null 2>&1; then PY=python
else PY=python3; fi

COUNT="$("$PY" - "$REPO" << 'PY'
import json, subprocess, sys

repo = sys.argv[1]
total = 0
page = 1
while page <= 50:
    proc = subprocess.run(
        ["gh", "api", f"repos/{repo}/dependabot/alerts?state=open&per_page=100&page={page}"],
        capture_output=True, text=True,
    )
    if proc.returncode != 0:
        err = (proc.stderr or proc.stdout or "unknown").strip()
        print(f"error: {err}", file=sys.stderr)
        raise SystemExit(1)
    alerts = json.loads(proc.stdout or "[]")
    if not alerts:
        break
    for a in alerts:
        sev = (a.get("security_vulnerability") or {}).get("severity", "").lower()
        if sev in ("critical", "high"):
            total += 1
    if len(alerts) < 100:
        break
    page += 1
print(total)
PY
)"

echo "$COUNT"
