#!/usr/bin/env bash
# Lint + smoke gate for active stack after feature work.
# Usage: scripts/feature-gate.sh [--json] [--stack dotnet-wpf|web|python|android|node|multi] [--step LABEL]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

if command -v python3 >/dev/null 2>&1; then PY=python3
elif command -v python >/dev/null 2>&1; then PY=python
else PY=python3; fi

JSON=false
STRICT=false
STACK=""
STEP=""
while [ $# -gt 0 ]; do
  case "$1" in
    --json) JSON=true; shift ;;
    --strict) STRICT=true; shift ;;
    --stack=*) STACK="${1#*=}"; shift ;;
    --stack) STACK="${2:-}"; shift 2 ;;
    --step=*) STEP="${1#*=}"; shift ;;
    --step) STEP="${2:-}"; shift 2 ;;
    *) shift ;;
  esac
done

log() {
  if [ "$JSON" = true ]; then
    echo "$@" >&2
  else
    echo "$@"
  fi
}

FAILED_STAGE=""
LOG_TAIL=""
SUGGESTED=()
GATES_PASSED=()

emit_json() {
  local ok="$1" code="$2"
  if [ "$JSON" = true ]; then
    local gp_json
    gp_json="$($PY -c 'import json,sys; print(json.dumps(sys.argv[1:]))' "${GATES_PASSED[@]}")"
    $PY - "$ok" "$code" "$FAILED_STAGE" "$LOG_TAIL" "$STEP" "$gp_json" "${SUGGESTED[@]}" << 'PY'
import json, sys
ok, code = sys.argv[1], int(sys.argv[2])
failed = sys.argv[3] if len(sys.argv) > 3 else ""
log_tail = sys.argv[4] if len(sys.argv) > 4 else ""
step = sys.argv[5] if len(sys.argv) > 5 else ""
gp = json.loads(sys.argv[6]) if len(sys.argv) > 6 and sys.argv[6] else []
fixes = sys.argv[7:] if len(sys.argv) > 7 else []
print(json.dumps({
    "ok": ok == "true",
    "exit_code": code,
    "step": step or None,
    "failed_stage": failed or None,
    "log_tail": log_tail[:2000] if log_tail else None,
    "gates_passed": gp,
    "suggested_fixes": fixes,
}, indent=2))
PY
  fi
}

record_progress() {
  local exit_code="$1"
  local gp=""
  if [ "${#GATES_PASSED[@]}" -gt 0 ]; then
    gp="$(IFS=,; echo "${GATES_PASSED[*]}")"
  fi
  local rec=(record --gate feature-gate --exit "$exit_code")
  [ -n "$STEP" ] && rec+=(--step "$STEP" --build-plan-step "$STEP")
  [ -n "$gp" ] && rec+=(--gates-passed "$gp")
  [ -n "$FAILED_STAGE" ] && rec+=(--failed-stage "$FAILED_STAGE")
  [ -n "$LOG_TAIL" ] && rec+=(--log-tail "$LOG_TAIL")
  bash scripts/agent-progress.sh "${rec[@]}" 2>/dev/null || true
}

fail_gate() {
  local stage="$1"
  local log_msg="${2:-}"
  FAILED_STAGE="$stage"
  LOG_TAIL="$log_msg"
  case "$stage" in
    dotnet-restore|dotnet-build|dotnet-test|dotnet-format)
      SUGGESTED=("fix build/test errors in QuickMediaIngest/" "run dotnet test QuickMediaIngest-1.sln -c Release") ;;
    dotnet-vulnerable)
      SUGGESTED=("run dotnet list package --vulnerable" "bump affected NuGet packages") ;;
    file-limits) SUGGESTED=("split oversized files per modules/dotnet-wpf/MODULE.md limits") ;;
    license) SUGGESTED=("run scripts/check-license-compliance.sh") ;;
    *) SUGGESTED=("run scripts/feature-autofix.sh" "run .\\scripts\\validate-local.ps1") ;;
  esac
  emit_json false 1
  record_progress 1
  exit 1
}

block_env() {
  FAILED_STAGE="environment"
  LOG_TAIL="$1"
  emit_json false 2
  record_progress 2
  exit 2
}

if [ -z "$STACK" ] && [ -f .cursor/stack-selection.json ]; then
  STACK="$($PY -c "import json; print(json.load(open('.cursor/stack-selection.json')).get('stack','dotnet-wpf'))" 2>/dev/null || echo dotnet-wpf)"
fi
STACK="${STACK:-dotnet-wpf}"

should_run() {
  local s="$1"
  [ "$STACK" = "multi" ] || [ "$STACK" = "none" ] || [ "$STACK" = "$s" ]
}

skip_or_block() {
  local msg="$1"
  if [ "$STRICT" = true ]; then
    block_env "$msg"
  fi
  log "$msg"
}

run_cmd() {
  local stage="$1"
  shift
  local logfile
  logfile="$(mktemp 2>/dev/null || echo /tmp/feature-gate-$$.log)"
  if "$@" >"$logfile" 2>&1; then
    GATES_PASSED+=("$stage")
    rm -f "$logfile"
    return 0
  fi
  fail_gate "$stage" "$(tail -n 40 "$logfile")"
}

log "Feature gate (stack=$STACK step=${STEP:-none} strict=$STRICT)..."

if [ -f scripts/check-repo-hygiene.sh ]; then
  if ! bash scripts/check-repo-hygiene.sh >/dev/null 2>&1; then
    fail_gate "hygiene" "$(bash scripts/check-repo-hygiene.sh 2>&1 | tail -n 20)"
  fi
  GATES_PASSED+=("hygiene")
fi

if [ -f scripts/sync-exemplar-config.sh ]; then
  bash scripts/sync-exemplar-config.sh >/dev/null 2>&1 || true
fi

if ! bash scripts/check-file-encoding.sh >/dev/null 2>&1; then
  fail_gate "encoding" "$(bash scripts/check-file-encoding.sh 2>&1 | tail -n 20)"
fi
GATES_PASSED+=("encoding")

if ! bash scripts/check-file-limits.sh >/dev/null 2>&1; then
  fail_gate "file-limits" "$(bash scripts/check-file-limits.sh 2>&1 | tail -n 20)"
fi
GATES_PASSED+=("file-limits")

if should_run dotnet-wpf && [ -f QuickMediaIngest-1.sln ]; then
  if ! command -v dotnet >/dev/null 2>&1; then
    block_env "dotnet SDK not found; install .NET 8 SDK"
  fi
  run_cmd dotnet-restore dotnet restore QuickMediaIngest-1.sln
  run_cmd dotnet-build dotnet build QuickMediaIngest-1.sln -c Release --no-restore
  run_cmd dotnet-test dotnet test QuickMediaIngest-1.sln -c Release --no-build --verbosity minimal
  run_cmd dotnet-format dotnet format QuickMediaIngest-1.sln --verify-no-changes
  run_cmd dotnet-vulnerable dotnet list QuickMediaIngest-1.sln package --vulnerable --include-transitive
  if [ -f scripts/check-license-compliance.sh ]; then
    run_cmd license bash scripts/check-license-compliance.sh
  fi
fi

if should_run web && [ -f examples/web/package.json ]; then
  if command -v npm >/dev/null 2>&1; then
    (cd examples/web && run_cmd web-lint npm run lint)
  else
    skip_or_block "Skipping web gate (npm not found)"
  fi
fi

log "Feature gate passed (${#GATES_PASSED[@]} stages)."
emit_json true 0
record_progress 0
exit 0
