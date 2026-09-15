#!/usr/bin/env bash
# AUTO LX-L5: inspect a built .deb. Optional --card lists removable mounts (no ingest).
# Exit 1 until LX-L4 produces an artifact. Never requires a person at the keyboard.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

CARD=false
DEB=""
for arg in "$@"; do
  case "$arg" in
    --card) CARD=true ;;
    --help|-h)
      echo "Usage: smoke-deb.sh [--card] [path-to-deb]"
      exit 0
      ;;
    *) DEB="$arg" ;;
  esac
done

if [ -z "$DEB" ]; then
  DEB="$(find "$ROOT/dist" "$ROOT/publish" "$ROOT/artifacts" -name 'quick-media-ingest_*.deb' 2>/dev/null | sort | tail -n 1 || true)"
fi

if [ -z "$DEB" ] || [ ! -f "$DEB" ]; then
  echo "FAIL: no quick-media-ingest_*.deb (blocked until LX-L4 pack-deb)"
  exit 1
fi

if [ "$(id -u)" -eq 0 ]; then
  echo "FAIL: refuse to smoke as uid 0"
  exit 1
fi

if command -v dpkg-deb >/dev/null 2>&1; then
  dpkg-deb -I "$DEB"
  set +o pipefail
  dpkg-deb -c "$DEB" | head -n 40
  set -o pipefail
else
  echo "SKIP dpkg-deb (not on PATH); artifact present: $DEB"
fi

if command -v lintian >/dev/null 2>&1; then
  lintian --fail-on error "$DEB"
else
  echo "SKIP lintian (not on PATH)"
fi

if command -v dpkg-deb >/dev/null 2>&1; then
  set +o pipefail
  if ! dpkg-deb -c "$DEB" | grep -q 'quick-media-ingest-import.desktop'; then
    echo "FAIL: deb missing import .desktop (x-content/image-dcf handler)"
    exit 1
  fi
  mime="$(dpkg-deb --fsys-tarfile "$DEB" | tar -xO ./usr/share/applications/quick-media-ingest-import.desktop)"
  echo "$mime" | grep -q 'x-content/image-dcf' || {
    echo "FAIL: import desktop missing x-content/image-dcf"
    exit 1
  }
  echo "$mime" | grep -q 'NoDisplay=true' && {
    echo "FAIL: import desktop is NoDisplay (Gtk AppChooser hides it)"
    exit 1
  }
  menu="$(dpkg-deb --fsys-tarfile "$DEB" | tar -xO ./usr/share/applications/quick-media-ingest.desktop)"
  echo "$menu" | grep -q 'x-content/image-dcf' || {
    echo "FAIL: menu desktop missing x-content/image-dcf"
    exit 1
  }
  echo "$menu" | grep -q 'Icon=quick-media-ingest' || {
    echo "FAIL: menu desktop missing Icon=quick-media-ingest"
    exit 1
  }
  dpkg-deb -c "$DEB" | grep -q 'usr/share/icons/hicolor/.*/quick-media-ingest.png' || {
    echo "FAIL: deb missing hicolor app icon"
    exit 1
  }
  echo "$menu" | grep -q 'NoDisplay=true' && {
    echo "FAIL: menu desktop is NoDisplay"
    exit 1
  }
  set -o pipefail
fi

if [ "$CARD" = true ]; then
  echo "Removable mounts:"
  ls -d /media/*/* /run/media/*/* 2>/dev/null || echo "(none)"
fi

echo "OK: smoke-deb $DEB"
