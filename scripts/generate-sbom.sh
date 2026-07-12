#!/usr/bin/env bash
# Generate a CycloneDX SBOM for the published portable output (or a given path) via Syft.
# Usage: scripts/generate-sbom.sh [path] [output.json]
# Requires: syft on PATH, or Docker with anchore/syft.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

SCAN_PATH="${1:-./publish/portable}"
VERSION="$(python -c "import re,pathlib; t=pathlib.Path('QuickMediaIngest/QuickMediaIngest.csproj').read_text(encoding='utf-8'); m=re.search(r'<Version>([^<]+)</Version>', t); print(m.group(1) if m else '0.0.0')" 2>/dev/null || echo "0.0.0")"
OUT="${2:-./publish/QuickMediaIngest-${VERSION}.cyclonedx.json}"

if [ ! -e "$SCAN_PATH" ]; then
  echo "ERROR: scan path not found: $SCAN_PATH" >&2
  echo "Publish first, e.g. build_local_test.bat or dotnet publish … -o ./publish/portable" >&2
  exit 1
fi

mkdir -p "$(dirname "$OUT")"

if command -v syft >/dev/null 2>&1; then
  syft "dir:${SCAN_PATH}" -o "cyclonedx-json=${OUT}"
elif command -v docker >/dev/null 2>&1; then
  ABS_SCAN="$(cd "$SCAN_PATH" && pwd)"
  ABS_OUT_DIR="$(cd "$(dirname "$OUT")" && pwd)"
  OUT_BASE="$(basename "$OUT")"
  docker run --rm -v "${ABS_SCAN}:/scan:ro" -v "${ABS_OUT_DIR}:/out" anchore/syft:v1.19.0 \
    "dir:/scan" -o "cyclonedx-json=/out/${OUT_BASE}"
else
  echo "ERROR: install Syft (https://github.com/anchore/syft) or Docker to generate SBOMs" >&2
  exit 1
fi

echo "Wrote $OUT"
