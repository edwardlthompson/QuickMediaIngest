#!/usr/bin/env bash
# Ensure gh can read Dependabot alerts (security_events scope or GITHUB_TOKEN in CI).
# Usage: scripts/ensure-gh-security-scope.sh [--strict] [--refresh]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

STRICT=false
REFRESH=false
while [ $# -gt 0 ]; do
  case "$1" in
    --strict) STRICT=true; shift ;;
    --refresh) REFRESH=true; shift ;;
    *) shift ;;
  esac
done

if ! command -v gh >/dev/null 2>&1; then
  echo "SKIP: gh CLI not installed"
  [ "$STRICT" = true ] && exit 1 || exit 0
fi

REPO="${GITHUB_REPOSITORY:-${GITHUB_REPO:-$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null || true)}}"
if [ -z "$REPO" ]; then
  echo "SKIP: gh not authenticated"
  [ "$STRICT" = true ] && exit 1 || exit 0
fi

probe_dependabot_api() {
  gh api "repos/${REPO}/dependabot/alerts" -f state=open -f per_page=1 >/dev/null 2>&1
}

if probe_dependabot_api; then
  echo "OK gh can read Dependabot alerts (${REPO})"
  exit 0
fi

if [ "$REFRESH" = true ] || [ "${QMI_GH_AUTH_REFRESH:-}" = "1" ]; then
  if [ -t 0 ] || [ "${QMI_GH_AUTH_REFRESH:-}" = "1" ]; then
    echo "Attempting gh auth refresh -s security_events (interactive browser may open)..."
    if gh auth refresh -s security_events; then
      if probe_dependabot_api; then
        echo "OK Dependabot API access after refresh"
        exit 0
      fi
    fi
  fi
fi

echo "WARN: Dependabot API unavailable — token lacks security_events scope"
echo "      Local fix: QMI_GH_AUTH_REFRESH=1 bash scripts/ensure-gh-security-scope.sh --refresh"
echo "      Or: gh auth refresh -s security_events"
echo "      CI: GITHUB_TOKEN with security-events permission works automatically"

if [ "$STRICT" = true ]; then
  exit 1
fi
exit 0
