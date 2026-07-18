#!/usr/bin/env bash
# Count open Critical/High Dependabot alerts.
# Usage: scripts/count-critical-high-dependabot.sh
# Exit 0 prints count to stdout; exit 1 on API/auth error.
#
# Note: Dependabot alerts API rejects `page=` (HTTP 400). Fetch open alerts
# without pagination params; for large repos follow Link headers via gh --paginate.
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
proc = subprocess.run(
    [
        "gh",
        "api",
        "--paginate",
        f"repos/{repo}/dependabot/alerts?state=open&per_page=100",
    ],
    capture_output=True,
    text=True,
)
if proc.returncode != 0:
    err = (proc.stderr or proc.stdout or "unknown").strip()
    print(f"error: {err}", file=sys.stderr)
    raise SystemExit(1)

raw = (proc.stdout or "").strip()
if not raw:
    print(0)
    raise SystemExit(0)

# --paginate may concatenate JSON arrays; normalize to one list.
alerts = []
decoder = json.JSONDecoder()
idx = 0
while idx < len(raw):
    while idx < len(raw) and raw[idx].isspace():
        idx += 1
    if idx >= len(raw):
        break
    chunk, offset = decoder.raw_decode(raw, idx)
    idx = offset
    if isinstance(chunk, list):
        alerts.extend(chunk)
    else:
        print("error: unexpected Dependabot API payload", file=sys.stderr)
        raise SystemExit(1)

total = 0
for a in alerts:
    sev = (a.get("security_vulnerability") or {}).get("severity", "").lower()
    if sev in ("critical", "high"):
        total += 1
print(total)
PY
)"

echo "$COUNT"
