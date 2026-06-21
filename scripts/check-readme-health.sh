#!/usr/bin/env bash
# README health: required sections and doc links referenced from README.md
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
README="$ROOT/README.md"
ERRORS=0

if [ ! -f "$README" ]; then
  echo "FAIL: README.md missing"
  exit 1
fi

require_section() {
  local heading="$1"
  if ! grep -qF "$heading" "$README"; then
    echo "FAIL: README missing section: $heading"
    ERRORS=$((ERRORS + 1))
  fi
}

require_file() {
  local rel="$1"
  if [ ! -f "$ROOT/$rel" ]; then
    echo "FAIL: README references missing file: $rel"
    ERRORS=$((ERRORS + 1))
  fi
}

require_section "## Key features"
require_section "## Build and development"
require_section "## License"
require_section "## Agent / Cursor quick start"
require_section "## Security"

require_file "docs/START_HERE.md"
require_file "docs/FOR_AGENTS.md"
require_file "AGENTS.md"
require_file "BUILD_PLAN.md"
require_file "SECURITY.md"
require_file "CONTRIBUTING.md"
require_file "LICENSE"
require_file "docs/THEME_QA_CHECKLIST.md"
require_file "QuickMediaIngest/QuickMediaIngest.csproj"

if ! grep -qF '<Version>' "$ROOT/QuickMediaIngest/QuickMediaIngest.csproj"; then
  echo "FAIL: QuickMediaIngest.csproj missing <Version>"
  ERRORS=$((ERRORS + 1))
fi

if [ "$ERRORS" -gt 0 ]; then
  echo "$ERRORS README health check(s) failed"
  exit 1
fi

echo "README health check passed"
