#!/usr/bin/env bash
# License compliance checks for dotnet stack
# Usage: check-license-compliance.sh [dotnet|all]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

STACK="${1:-all}"
ERRORS=0

if [ ! -f LICENSE ]; then
  echo "MISSING: LICENSE"
  ERRORS=$((ERRORS + 1))
fi

if [ ! -f THIRD_PARTY_LICENSES.md ]; then
  echo "MISSING: THIRD_PARTY_LICENSES.md"
  ERRORS=$((ERRORS + 1))
fi

check_dotnet() {
  if [ ! -f QuickMediaIngest/QuickMediaIngest.csproj ]; then
    echo "ERROR: QuickMediaIngest.csproj not found"
    ERRORS=$((ERRORS + 1))
    return
  fi
  echo "Dotnet license check: THIRD_PARTY_LICENSES.md present"
  echo "Review NuGet licenses manually or via dotnet list package before release"
}

case "$STACK" in
  dotnet) check_dotnet ;;
  all) check_dotnet ;;
  *)
    echo "Usage: $0 [dotnet|all]"
    exit 1
    ;;
esac

if [ "$ERRORS" -gt 0 ]; then
  exit 1
fi

echo "License compliance check passed"
