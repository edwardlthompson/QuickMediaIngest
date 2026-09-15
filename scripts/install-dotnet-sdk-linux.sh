#!/usr/bin/env bash
# Install .NET 8 SDK to $HOME/.dotnet (no sudo). Official Microsoft installer.
set -euo pipefail
INSTALL_DIR="${DOTNET_INSTALL_DIR:-${DOTNET_ROOT:-$HOME/.dotnet}}"
CHANNEL="${DOTNET_CHANNEL:-8.0}"
URL="https://dot.net/v1/dotnet-install.sh"
INSTALLER="$(mktemp)"
cleanup() { rm -f "$INSTALLER"; }
trap cleanup EXIT

if command -v curl >/dev/null 2>&1; then
  curl -fsSL "$URL" -o "$INSTALLER"
elif command -v wget >/dev/null 2>&1; then
  wget -qO "$INSTALLER" "$URL"
else
  echo "FAIL: need curl or wget to download dotnet-install.sh" >&2
  exit 1
fi

bash "$INSTALLER" --channel "$CHANNEL" --install-dir "$INSTALL_DIR"
export DOTNET_ROOT="$INSTALL_DIR"
export PATH="$INSTALL_DIR:${PATH}"
"$INSTALL_DIR/dotnet" --info
echo "OK: .NET SDK at $INSTALL_DIR (add to PATH or rely on feature-gate DOTNET_ROOT lookup)"
