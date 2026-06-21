#!/usr/bin/env bash
# Automate BUILD_PLAN HUMAN sign-off items (tests, smoke, security triage, optional CI/push).
#
# Usage:
#   bash scripts/run-human-signoffs.sh
#   bash scripts/run-human-signoffs.sh --strict --wait-ci 300
#   bash scripts/run-human-signoffs.sh --refresh-gh --published-exe
#   QMI_ALLOW_PUSH=1 bash scripts/run-human-signoffs.sh --push
#
# Exit 0 when all automated checks pass; 1 on failure. Interactive gh refresh is optional.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

STRICT=false
WAIT_CI=0
REFRESH_GH=false
PUBLISHED_EXE=false
PUSH=false
SKIP_SECURITY=false

while [ $# -gt 0 ]; do
  case "$1" in
    --strict) STRICT=true; shift ;;
    --wait-ci) WAIT_CI="${2:-300}"; shift 2 ;;
    --refresh-gh) REFRESH_GH=true; shift ;;
    --published-exe) PUBLISHED_EXE=true; shift ;;
    --push) PUSH=true; shift ;;
    --skip-security) SKIP_SECURITY=true; shift ;;
    *) echo "Unknown arg: $1"; exit 2 ;;
  esac
done

FAILURES=0
step() {
  echo ""
  echo "=== $1 ==="
}

run_step() {
  local label="$1"
  shift
  step "$label"
  if "$@"; then
    echo "OK $label"
  else
    echo "FAIL $label"
    FAILURES=$((FAILURES + 1))
    if [ "$STRICT" = true ]; then
      exit 1
    fi
  fi
}

SIGNOFF_FILTER='FullyQualifiedName~HumanSignoffVerificationTests|FullyQualifiedName~DeleteAfterImportConfirmHelperTests|FullyQualifiedName~MainViewModelConfigReloadTests|FullyQualifiedName~ConfigFilePersistenceTests|FullyQualifiedName~HumanVerificationSmokeTests|FullyQualifiedName~FtpThumbnail'

run_step "Restore + build" bash -c 'dotnet restore QuickMediaIngest-1.sln && dotnet build QuickMediaIngest-1.sln -c Release --no-restore'

run_step "HUMAN sign-off tests (UI bindings + config + optional LAN FTP)" \
  dotnet test QuickMediaIngest-1.sln -c Release --no-build --filter "$SIGNOFF_FILTER" --verbosity normal

run_step "Full unit test baseline" \
  dotnet test QuickMediaIngest-1.sln -c Release --no-build --verbosity minimal

if [ "$PUBLISHED_EXE" = true ]; then
  run_step "Published exe libvips smoke" bash scripts/smoke-published-exe.sh
fi

if [ "$SKIP_SECURITY" = false ]; then
  step "GitHub Dependabot scope"
  GH_ARGS=()
  [ "$REFRESH_GH" = true ] && GH_ARGS+=(--refresh)
  [ "$STRICT" = true ] && GH_ARGS+=(--strict)
  if bash scripts/ensure-gh-security-scope.sh "${GH_ARGS[@]}"; then
    echo "OK Dependabot API scope"
    step "Security triage (strict=${STRICT})"
    TRIAGE_ARGS=()
    [ "$STRICT" = true ] && TRIAGE_ARGS+=(--strict)
    [ "$WAIT_CI" -gt 0 ] && TRIAGE_ARGS+=(--wait-ci "$WAIT_CI")
    if bash scripts/check-security-triage.sh "${TRIAGE_ARGS[@]}"; then
      echo "OK security triage"
    else
      echo "FAIL security triage"
      FAILURES=$((FAILURES + 1))
      [ "$STRICT" = true ] && exit 1
    fi
  else
    echo "WARN security triage skipped (Dependabot scope)"
    [ "$STRICT" = true ] && exit 1
  fi
fi

if [ "$PUSH" = true ]; then
  step "Git push (requires QMI_ALLOW_PUSH=1)"
  if [ "${QMI_ALLOW_PUSH:-}" != "1" ]; then
    echo "FAIL: set QMI_ALLOW_PUSH=1 to enable automated push"
    FAILURES=$((FAILURES + 1))
  elif ! git diff --quiet || ! git diff --cached --quiet || [ -n "$(git status --porcelain)" ]; then
    echo "FAIL: uncommitted changes — commit first (F-001)"
    FAILURES=$((FAILURES + 1))
  else
    BRANCH="$(git rev-parse --abbrev-ref HEAD)"
    if git push -u origin "$BRANCH"; then
      echo "OK pushed $BRANCH"
      if [ "$WAIT_CI" -gt 0 ] && command -v gh >/dev/null 2>&1; then
        run_step "CI green on pushed HEAD" bash scripts/check-github-ci.sh HEAD --wait "$WAIT_CI"
      fi
    else
      echo "FAIL git push"
      FAILURES=$((FAILURES + 1))
    fi
  fi
fi

echo ""
if [ "$FAILURES" -gt 0 ]; then
  echo "$FAILURES human sign-off step(s) failed"
  exit 1
fi

echo "All automated human sign-off checks passed."
echo "Optional manual only: glance at live UI if you want visual confirmation beyond headless tests."
