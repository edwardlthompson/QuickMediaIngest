#!/usr/bin/env bash
# Automated smoke for BUILD_PLAN HUMAN verification rows (skips when LAN FTP offline).
# Usage:
#   bash scripts/smoke-human-verification.sh
#   QMI_SMOKE_REQUIRE=1 bash scripts/smoke-human-verification.sh   # fail if FTP unreachable
#   bash scripts/smoke-human-verification.sh --no-security
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

RUN_SECURITY=true
for arg in "$@"; do
  case "$arg" in
    --no-security) RUN_SECURITY=false ;;
    --require-ftp) export QMI_SMOKE_REQUIRE=1 ;;
  esac
done

echo "=== Human verification smoke (config + optional LAN FTP) ==="

dotnet restore QuickMediaIngest-1.sln
dotnet build QuickMediaIngest-1.sln -c Release --no-restore

FILTER='FullyQualifiedName~ConfigFilePersistenceTests|FullyQualifiedName~HumanVerificationSmokeTests|FullyQualifiedName~FtpThumbnail'
dotnet test QuickMediaIngest-1.sln -c Release --no-build --filter "$FILTER" --verbosity normal

if [ "$RUN_SECURITY" = true ] && command -v gh >/dev/null 2>&1; then
  echo "=== Security triage (automated; non-fatal when Dependabot scope missing) ==="
  bash scripts/check-security-triage.sh || echo "WARN: security triage incomplete — see docs/SECURITY_TRIAGE.md"
fi

echo "Human verification smoke passed."
