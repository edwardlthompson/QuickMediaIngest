#!/usr/bin/env bash
# LX-L4/LX-trim: self-contained trimmed ReadyToRun multi-file .deb (linux-x64).
# Intended for ubuntu-latest / Debian-family hosts. Does not overwrite docs/spec.md or docs/plan.md.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

if [ "$(uname -s)" != "Linux" ]; then
  echo "SKIP: pack-deb.sh is Linux-only (ubuntu-latest)"
  exit 0
fi

if ! command -v dpkg-deb >/dev/null 2>&1; then
  echo "FAIL: dpkg-deb not on PATH"
  exit 1
fi

DOTNET="${DOTNET_ROOT:-$HOME/.dotnet}/dotnet"
if [ ! -x "$DOTNET" ]; then
  DOTNET="$(command -v dotnet || true)"
fi
if [ -z "$DOTNET" ]; then
  echo "FAIL: dotnet SDK not found"
  exit 1
fi

VERSION="${QMI_DEB_VERSION:-1.5.0}"
STAGE="$ROOT/dist/deb-stage"
OUT="$ROOT/dist"
DEB="$OUT/quick-media-ingest_${VERSION}_amd64.deb"
APPDIR="$STAGE/opt/quick-media-ingest"

rm -rf "$STAGE"
mkdir -p "$APPDIR" "$STAGE/DEBIAN" \
  "$STAGE/usr/share/applications" \
  "$STAGE/usr/share/metainfo" \
  "$STAGE/usr/share/doc/quick-media-ingest" \
  "$STAGE/usr/share/icons/hicolor/256x256/apps" \
  "$STAGE/usr/share/pixmaps" \
  "$OUT"

"$DOTNET" publish QuickMediaIngest.Desktop/QuickMediaIngest.Desktop.csproj \
  -c Release -r linux-x64 --self-contained true \
  -p:PublishReadyToRun=true \
  -p:PublishSingleFile=false \
  -p:PublishTrimmed=true \
  -p:DebugType=none \
  -p:DebugSymbols=false \
  -o "$APPDIR"
rm -f "$APPDIR"/*.pdb

SMOKE_BIN="$APPDIR/QuickMediaIngest.Desktop"
if [ ! -f "$SMOKE_BIN" ]; then
  echo "FAIL: trimmed publish missing $SMOKE_BIN"
  exit 1
fi
chmod +x "$SMOKE_BIN"
"$SMOKE_BIN" --smoke-native
find "$APPDIR" -type f \( -name '*.so' -o -name '*.so.*' \) -exec chmod 0644 {} +

{
  cat packaging/debian/control
  printf 'Version: %s\n' "$VERSION"
} > "$STAGE/DEBIAN/control"

cat > "$STAGE/DEBIAN/postinst" << 'EOF'
#!/bin/sh
set -e
chmod 0755 /opt/quick-media-ingest/QuickMediaIngest.Desktop || true
if command -v update-desktop-database >/dev/null 2>&1; then
  update-desktop-database -q /usr/share/applications || true
fi
if command -v gtk-update-icon-cache >/dev/null 2>&1; then
  gtk-update-icon-cache -f -t /usr/share/icons/hicolor >/dev/null 2>&1 || true
fi
exit 0
EOF
chmod 0755 "$STAGE/DEBIAN/postinst"

install -m 0644 packaging/debian/quick-media-ingest.desktop "$STAGE/usr/share/applications/quick-media-ingest.desktop"
install -m 0644 packaging/debian/quick-media-ingest-import.desktop \
  "$STAGE/usr/share/applications/quick-media-ingest-import.desktop"
ICON_SRC="$ROOT/QuickMediaIngest/Assets/AppIcon.png"
ICON_DST="$STAGE/usr/share/icons/hicolor/256x256/apps/quick-media-ingest.png"
if command -v convert >/dev/null 2>&1 && convert -version 2>/dev/null | grep -q ImageMagick; then
  convert "$ICON_SRC" -resize 256x256 "$ICON_DST"
elif command -v magick >/dev/null 2>&1 && magick -version 2>/dev/null | grep -q ImageMagick; then
  magick "$ICON_SRC" -resize 256x256 "$ICON_DST"
else
  mkdir -p "$STAGE/usr/share/icons/hicolor/1024x1024/apps"
  install -m 0644 "$ICON_SRC" "$STAGE/usr/share/icons/hicolor/1024x1024/apps/quick-media-ingest.png"
  install -m 0644 "$ICON_SRC" "$ICON_DST"
fi
install -m 0644 "$ICON_DST" "$STAGE/usr/share/pixmaps/quick-media-ingest.png"
install -m 0644 packaging/debian/io.github.edwardlthompson.QuickMediaIngest.metainfo.xml \
  "$STAGE/usr/share/metainfo/io.github.edwardlthompson.QuickMediaIngest.metainfo.xml"
install -m 0644 packaging/debian/copyright "$STAGE/usr/share/doc/quick-media-ingest/copyright"
install -d "$STAGE/usr/share/lintian/overrides"
install -m 0644 packaging/debian/lintian-overrides \
  "$STAGE/usr/share/lintian/overrides/quick-media-ingest"
{
  printf 'quick-media-ingest (%s) unstable; urgency=medium\n\n' "$VERSION"
  printf '  * Release %s.\n\n' "$VERSION"
  printf ' -- Edward Thompson <noreply@users.noreply.github.com>  %s\n' "$(date -uR)"
} | gzip -9n > "$STAGE/usr/share/doc/quick-media-ingest/changelog.gz"

dpkg-deb --root-owner-group --build "$STAGE" "$DEB"

if command -v lintian >/dev/null 2>&1; then
  lintian --fail-on error "$DEB"
else
  echo "SKIP lintian (not on PATH); CI ubuntu-latest should install it"
fi

echo "OK: $DEB"
