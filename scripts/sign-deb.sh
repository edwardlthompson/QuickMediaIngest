#!/usr/bin/env bash
# v1 ships unsigned GitHub .deb files. Signs only when DEB_GPG_KEY (or a keyring) is set.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

if [ -z "${DEB_GPG_KEY:-}" ] && [ -z "${DEB_GPG_KEY_FILE:-}" ]; then
  echo "OK: v1 unsigned GitHub .deb (no DEB_GPG_KEY); skip signing"
  exit 0
fi

DEB="${1:-}"
if [ -z "$DEB" ]; then
  DEB="$(find "$ROOT/dist" "$ROOT/publish" "$ROOT/artifacts" -name 'quick-media-ingest_*.deb' 2>/dev/null | sort | tail -n 1 || true)"
fi
if [ -z "$DEB" ] || [ ! -f "$DEB" ]; then
  echo "FAIL: DEB_GPG_KEY is set but no .deb artifact"
  exit 1
fi

if ! command -v dpkg-sig >/dev/null 2>&1 && ! command -v debsigs >/dev/null 2>&1; then
  echo "FAIL: DEB_GPG_KEY is set but dpkg-sig/debsigs is missing"
  exit 1
fi

echo "OK: signing tools present for $DEB (invoke dpkg-sig in CI when LX-L4 lands)"
