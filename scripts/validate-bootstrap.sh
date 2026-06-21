#!/usr/bin/env bash
# Verify required bootstrap artifacts exist and pass delegated checks
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
if [ -f .cursor/stack-selection.json ]; then
  STACK="$($PY -c "import json; print(json.load(open('.cursor/stack-selection.json')).get('stack','dotnet-wpf'))" 2>/dev/null || echo dotnet-wpf)"
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
  docs/START_HERE.md
  docs/CURSOR_MODES.md
  docs/INITIALIZATION_PROMPT.md
  .cursor/rules/cursor-modes.mdc
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
)

BATCH_COMMANDS=(
  audit debug gates triage dependabot push prerelease regress
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

run_check bash scripts/check-file-encoding.sh
if [ -f scripts/check-batch-commands.sh ]; then
  run_check bash scripts/check-batch-commands.sh
fi
if [ -f scripts/check-repo-hygiene.sh ]; then
  run_check bash scripts/check-repo-hygiene.sh
fi
if [ -f scripts/check-markdown-tables.sh ]; then
  run_check bash scripts/check-markdown-tables.sh
fi
if [ -f scripts/check-changelog-unreleased.sh ]; then
  run_check bash scripts/check-changelog-unreleased.sh
fi
if [ -f scripts/check-template-version-sync.sh ]; then
  run_check bash scripts/check-template-version-sync.sh
fi

if [ "$QUICK" = false ] && [ -f scripts/validate-workflow-actions.sh ]; then
  run_check bash scripts/validate-workflow-actions.sh
fi

run_check bash scripts/validate-template-index.sh

if [ "$ERRORS" -gt 0 ]; then
  echo "$ERRORS bootstrap check(s) failed"
  exit 1
fi

if [ "$QUICK" = true ]; then
  echo "Bootstrap validation passed (--quick: skipped validate-workflow-actions)"
else
  echo "Bootstrap validation passed (stack=$STACK)"
fi
