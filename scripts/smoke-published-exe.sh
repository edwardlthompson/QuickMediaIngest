#!/usr/bin/env bash
# Smoke-test published portable exe (libvips native DLL bundle).
# Usage:
#   bash scripts/smoke-published-exe.sh
#   bash scripts/smoke-published-exe.sh --rebuild
#   QMI_SMOKE_REQUIRE_PUBLISHED=1 bash scripts/smoke-published-exe.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

REBUILD=false
for arg in "$@"; do
  case "$arg" in
    --rebuild) REBUILD=true ;;
    --require) export QMI_SMOKE_REQUIRE_PUBLISHED=1 ;;
  esac
done

EXE="$ROOT/publish/local-test/QuickMediaIngest.exe"

if [ "$REBUILD" = true ] || [ ! -f "$EXE" ]; then
  echo "==> Building published portable exe"
  cmd //c build_local_test.bat
fi

if [ ! -f "$EXE" ]; then
  if [ "${QMI_SMOKE_REQUIRE_PUBLISHED:-0}" = "1" ]; then
    echo "FAIL: published exe missing at $EXE"
    exit 1
  fi
  echo "SKIP: published exe not found at $EXE"
  exit 0
fi

echo "==> Headless libvips smoke: $EXE --smoke-libvips"
export QMI_SMOKE_PUBLISHED_EXE="$EXE"
"$EXE" --smoke-libvips"

dotnet test QuickMediaIngest-1.sln -c Release --filter FullyQualifiedName~PublishedExeSmokeTests --verbosity minimal

echo "Published exe smoke passed."
