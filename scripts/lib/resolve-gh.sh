# shellcheck shell=bash
# Source from other scripts:  # shellcheck source=scripts/lib/resolve-gh.sh
#   . "$(cd "$(dirname "$0")" && pwd)/lib/resolve-gh.sh"   # when sourced from scripts/
#   . "$ROOT/scripts/lib/resolve-gh.sh"
#
# Defines `gh` function that prefers Windows gh.exe under WSL/Git Bash.

_qmi_resolve_gh_bin() {
  if command -v gh >/dev/null 2>&1; then
    command -v gh
    return 0
  fi
  local cand
  for cand in \
    "/mnt/c/Program Files/GitHub CLI/gh.exe" \
    "/c/Program Files/GitHub CLI/gh.exe" \
    "/usr/bin/gh" \
    "/usr/local/bin/gh"
  do
    if [ -f "$cand" ]; then
      echo "$cand"
      return 0
    fi
  done
  return 1
}

QMI_GH_BIN="$(_qmi_resolve_gh_bin || true)"

gh() {
  if [ -n "${QMI_GH_BIN:-}" ]; then
    "$QMI_GH_BIN" "$@"
  else
    command gh "$@"
  fi
}

qmi_gh_available() {
  [ -n "${QMI_GH_BIN:-}" ] || command -v gh >/dev/null 2>&1
}
