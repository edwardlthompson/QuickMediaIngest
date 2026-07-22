#!/usr/bin/env bash
# Verify required bootstrap artifacts exist and pass delegated checks
# QMI merge: keep dotnet-wpf paths; add FOSS cursor checks; skip web-only artifacts
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

if command -v python3 >/dev/null 2>&1; then PY=python3
elif command -v python >/dev/null 2>&1; then PY=python
else PY=python3; fi

QUICK=false
for arg in "$@"; do
  case "$arg" in
    --quick) QUICK=true ;;
  esac
done

STACK="dotnet-wpf"
TIER="foss"
if [ -f .cursor/stack-selection.json ]; then
  STACK="$($PY -c "import json; print(json.load(open('.cursor/stack-selection.json')).get('stack','dotnet-wpf'))" 2>/dev/null || echo dotnet-wpf)"
  TIER="$($PY -c "import json; print(json.load(open('.cursor/stack-selection.json')).get('distribution_tier','foss'))" 2>/dev/null || echo foss)"
fi

REQUIRED=(
  README.md
  LICENSE
  CONTRIBUTING.md
  SECURITY.md
  CODE_OF_CONDUCT.md
  BUILD_PLAN.md
  AGENTS.md
  AGENT_MEMORY.md
  HUMAN_BACKLOG.md
  docs/START_HERE.md
  docs/CURSOR_MODES.md
  docs/INITIALIZATION_PROMPT.md
  docs/BOOTSTRAP_ALIGNMENT.md
  docs/REPO_HYGIENE.md
  docs/FEATURE_MODULES.md
  docs/PARALLEL_AGENT_SCOPES.md
  docs/CURSOR_INTEGRATIONS.md
  .cursor/rules/cursor-modes.mdc
  .cursor/rules/local-compute.mdc
  .cursor/rules/wpf-mvvm.mdc
  docs/SECURITY_TRIAGE.md
  docs/THREAT_MODEL.md
  docs/PRIVACY.md
  docs/RUNBOOK.md
  docs/help/BATCH_COMMANDS.md
  docs/BATCH_COMMANDS.md
  .cursor/rules/batch-commands.mdc
  .github/dependabot.yml
  .github/CODEOWNERS
  THIRD_PARTY_LICENSES.md
  .env.example
  modules/dotnet-wpf/MODULE.md
  TEMPLATE_INDEX.json
  .cursor/stack-selection.json
  .cursor/hooks.json
  .cursor-session-state.example.json
  CODE_REVIEW.md.example
  RELEASE_NOTES.md.example
)

BATCH_COMMANDS=(
  audit cleanup debug gates triage dependabot push prerelease regress
  feature fix init prune ci docs upgrade setup plan restore compact scope
  bootstrap verify build ship maintain
)

for cmd in "${BATCH_COMMANDS[@]}"; do
  REQUIRED+=(".cursor/commands/${cmd}.md")
done

# Web-only artifacts — skip for dotnet-wpf stack
if [ "$STACK" != "dotnet-wpf" ]; then
  REQUIRED+=(
    docs/DESIGN_GUIDE.md
    docs/WEB_PROJECT_LAYOUT.md
    design-tokens/design-tokens.json
  )
fi

ERRORS=0

run_check() {
  if ! "$@"; then
    ERRORS=$((ERRORS + 1))
  fi
}

for f in "${REQUIRED[@]}"; do
  if [ ! -e "$f" ]; then
    echo "MISSING: $f"
    ERRORS=$((ERRORS + 1))
  fi
done

if [ -f LICENSE ] && [ ! -s LICENSE ]; then
  echo "EMPTY: LICENSE"
  ERRORS=$((ERRORS + 1))
fi

if ! grep -q 'QuickMediaIngest.Tests' QuickMediaIngest-1.sln 2>/dev/null; then
  echo "MISSING: QuickMediaIngest.Tests in QuickMediaIngest-1.sln"
  ERRORS=$((ERRORS + 1))
fi

if ! grep -q '\[AGENT\]' BUILD_PLAN.md && ! grep -q '\[HUMAN\]' BUILD_PLAN.md; then
  echo "MISSING: BUILD_PLAN.md owner labels"
  ERRORS=$((ERRORS + 1))
fi

# .cursorignore must NOT block agent rules or slash commands (ignore comment lines)
if [ -f .cursorignore ]; then
  while IFS= read -r line; do
    [[ "$line" =~ ^[[:space:]]*# ]] && continue
    [[ -z "${line// /}" ]] && continue
    for pattern in ".cursor/rules" ".cursor/commands" "*.mdc"; do
      if [[ "$line" == *"$pattern"* ]]; then
        echo "BLOCKED: .cursorignore rule '$line' matches '$pattern' — rules/commands must remain visible"
        ERRORS=$((ERRORS + 1))
      fi
    done
  done < .cursorignore
fi

# Independent read-only checks (parallel when helper present)
PARALLEL_CHECKS=(
  check-file-encoding.sh
  check-markdown-tables.sh
  check-changelog-unreleased.sh
  check-repo-hygiene.sh
  check-batch-commands.sh
  check-cursor-hooks.sh
  check-template-version-sync.sh
  validate-template-index.sh
)
if [ -f scripts/lib/run_checks_parallel.py ]; then
  if ! "$PY" scripts/lib/run_checks_parallel.py "${PARALLEL_CHECKS[@]}"; then
    ERRORS=$((ERRORS + 1))
  fi
else
  for c in "${PARALLEL_CHECKS[@]}"; do
    if [ -f "scripts/$c" ]; then
      run_check bash "scripts/$c"
    fi
  done
fi

if [ -f scripts/sync-cursor-features.py ]; then
  "$PY" scripts/sync-cursor-features.py --root "$ROOT" --tier "$TIER" || true
fi
if [ -f scripts/check-cursor-integrations.sh ]; then
  run_check bash scripts/check-cursor-integrations.sh --tier "$TIER"
fi

if [ "$QUICK" = false ] && [ -f scripts/validate-workflow-actions.sh ]; then
  run_check bash scripts/validate-workflow-actions.sh
fi

if [ "$ERRORS" -gt 0 ]; then
  echo "$ERRORS bootstrap check(s) failed"
  exit 1
fi

if [ "$QUICK" = true ]; then
  echo "Bootstrap validation passed (--quick: skipped validate-workflow-actions)"
else
  echo "Bootstrap validation passed (stack=$STACK tier=$TIER)"
fi
