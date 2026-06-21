#!/usr/bin/env bash
# Pre-release gate: CI green, zero Critical/High Dependabot alerts, template version present.
# Usage: scripts/pre-release-gate.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

ERRORS=0
VERSION=""
STACK="dotnet-wpf"
if [ -f .cursor/stack-selection.json ]; then
  STACK="$(python -c "import json; print(json.load(open('.cursor/stack-selection.json')).get('stack','dotnet-wpf'))" 2>/dev/null || echo dotnet-wpf)"
fi

echo "=== Pre-release gate (stack=$STACK) ==="

if ! bash scripts/feature-gate.sh --stack "$STACK" --strict --json; then
  echo "FAIL: feature-gate.sh"
  ERRORS=$((ERRORS + 1))
else
  echo "OK   feature-gate.sh passed"
fi

if [ -f scripts/check-security-triage.sh ]; then
  if ! bash scripts/check-security-triage.sh --wait-ci 300 --strict; then
    echo "FAIL: check-security-triage.sh --strict"
    ERRORS=$((ERRORS + 1))
  else
    echo "OK   check-security-triage.sh --strict passed"
  fi
else
  echo "WARN: check-security-triage.sh not found — run manual CVE triage per docs/SECURITY_TRIAGE.md"
fi

if [ ! -f .template-version ]; then
  echo "MISSING: .template-version"
  ERRORS=$((ERRORS + 1))
else
  VERSION="$(tr -d '[:space:]' < .template-version)"
  echo "OK   .template-version = ${VERSION}"
fi

if ! bash scripts/check-license-compliance.sh; then
  echo "FAIL: check-license-compliance.sh"
  ERRORS=$((ERRORS + 1))
else
  echo "OK   check-license-compliance.sh passed"
fi

echo ""
if [ "$ERRORS" -gt 0 ]; then
  echo "${ERRORS} pre-release gate check(s) failed"
  exit 1
fi

echo "Pre-release gate passed"
