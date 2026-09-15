#!/usr/bin/env bash
# T10: Trivy is required; gitleaks/semgrep stay advisory; local config gates exist.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
# shellcheck source=lib/resolve-python.sh
. "$(cd "$(dirname "$0")" && pwd)/lib/resolve-python.sh"
exec "$PY" "$ROOT/scripts/lib/security_scan_policy.py"
