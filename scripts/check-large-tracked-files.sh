#!/usr/bin/env bash
# Fail if any tracked file exceeds size budget (matches pre-commit 500KB gate)
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

MAX_KB=500
MAX_BYTES=$((MAX_KB * 1024))
ERRORS=0
MAX_REPORT=20
reported=0

if command -v python3 >/dev/null 2>&1; then PY=python3
elif command -v python >/dev/null 2>&1; then PY=python
else PY=""
fi

if [ -n "$PY" ]; then
  $PY - << 'PY'
import subprocess, sys

MAX_BYTES = 500 * 1024
errors = 0
try:
    out = subprocess.check_output(["git", "ls-tree", "-r", "-l", "HEAD"], text=True, encoding="utf-8", errors="replace")
    for line in out.splitlines():
        parts = line.split()
        if len(parts) >= 5:
            size_str = parts[3].strip()
            path = " ".join(parts[4:]).strip()
            if path.startswith("QuickMediaIngest/Assets/"):
                continue
            if size_str.isdigit() and int(size_str) > MAX_BYTES:
                kb = int(size_str) // 1024
                print(f"LARGE TRACKED FILE: {path} ({kb} KB > 500 KB)")
                errors += 1
except Exception as e:
    pass

if errors > 0:
    print(f"{errors} tracked file(s) exceed 500 KB")
    sys.exit(1)
print("Large tracked file check passed")
PY
  exit $?
fi

if [ "$ERRORS" -gt 0 ]; then
  echo "$ERRORS tracked file(s) exceed ${MAX_KB} KB"
  exit 1
fi

echo "Large tracked file check passed"
