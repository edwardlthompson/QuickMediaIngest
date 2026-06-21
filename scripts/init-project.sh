#!/usr/bin/env bash
# Post-template clone customization helper
# Usage: scripts/init-project.sh [options]
# --stack web|python|android|node|dotnet-wpf|multi|none
# --project-name NAME --purpose TEXT --interval INTERVAL
# --release-repo OWNER/REPO --donation-url URL --codeowner USER
# --prune --no-prune --non-interactive --reference --keep-optional --prune-optional
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

if command -v python3 >/dev/null 2>&1; then PY=python3
elif command -v python >/dev/null 2>&1; then PY=python
else PY=python3; fi

usage() {
  cat <<'EOF'
Usage: scripts/init-project.sh [options]
  --stack STACK          web|python|android|node|dotnet-wpf|multi|none
  --project-name NAME
  --purpose TEXT
  --interval INTERVAL    off|daily|weekly|monthly|on_session
  --release-repo OWNER/REPO
  --donation-url URL
  --codeowner USER       GitHub username without @
  --prune                Prune unused examples/modules without prompting
  --no-prune             Never prune (overrides --prune)
  --non-interactive      Skip prompts (requires --stack, --project-name, --purpose)
  --reference            Reference mode: skip placeholder/about/codeowner overwrites
  --keep-optional        When pruning, keep rust/go/lightroom examples (default)
  --prune-optional       When pruning, also remove optional stacks
  -h, --help
EOF
}

STACK=""
PROJECT_NAME=""
PROJECT_PURPOSE=""
INTERVAL=""
RELEASE_REPO=""
DONATION_URL=""
CODEOWNER=""
PRUNE_FLAG=""
NONINTERACTIVE=false
REFERENCE=false
KEEP_OPTIONAL=true
while [ $# -gt 0 ]; do
  case "$1" in
    --stack) STACK="${2:-}"; shift 2 ;;
    --project-name) PROJECT_NAME="${2:-}"; shift 2 ;;
    --purpose) PROJECT_PURPOSE="${2:-}"; shift 2 ;;
    --interval) INTERVAL="${2:-}"; shift 2 ;;
    --release-repo) RELEASE_REPO="${2:-}"; shift 2 ;;
    --donation-url) DONATION_URL="${2:-}"; shift 2 ;;
    --codeowner) CODEOWNER="${2:-}"; shift 2 ;;
    --prune) PRUNE_FLAG="yes"; shift ;;
    --no-prune) PRUNE_FLAG="no"; shift ;;
    --non-interactive) NONINTERACTIVE=true; shift ;;
    --reference) REFERENCE=true; shift ;;
    --keep-optional) KEEP_OPTIONAL=true; shift ;;
    --prune-optional) KEEP_OPTIONAL=false; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown option: $1" >&2; usage >&2; exit 1 ;;
  esac
done

prune_optional_stacks() {
  if [ "$KEEP_OPTIONAL" = true ]; then
    return 0
  fi
  rm -rf examples/rust examples/go examples/lightroom modules/rust modules/go modules/lightroom 2>/dev/null || true
}

prune_primary_stack() {
  local stack="$1"
  case "$stack" in
    web) rm -rf examples/python examples/android examples/node modules/python modules/android modules/node 2>/dev/null || true ;;
    python) rm -rf examples/web examples/android examples/node modules/web modules/android modules/node 2>/dev/null || true ;;
    android) rm -rf examples/web examples/python examples/node modules/web modules/python modules/node 2>/dev/null || true ;;
    node) rm -rf examples/web examples/python examples/android modules/web modules/python modules/android 2>/dev/null || true ;;
    dotnet-wpf)
      rm -rf examples 2>/dev/null || true
      rm -rf modules/web modules/python modules/android modules/node modules/lightroom modules/rust modules/go 2>/dev/null || true
      ;;
  esac
  prune_optional_stacks
}

if [ "$NONINTERACTIVE" = true ]; then
  if [ -z "$STACK" ] || [ -z "$PROJECT_NAME" ] || [ -z "$PROJECT_PURPOSE" ]; then
    echo "ERROR: --non-interactive requires --stack, --project-name, and --purpose" >&2
    exit 1
  fi
fi

echo "=== agent-project-bootstrap init ==="
if [ "$REFERENCE" = true ]; then
  echo "(Reference mode — preserving customized docs)"
fi
echo ""

if [ -z "$PROJECT_NAME" ] && [ "$NONINTERACTIVE" != true ]; then
  read -rp "Project name: " PROJECT_NAME
fi
if [ -z "$PROJECT_PURPOSE" ] && [ "$NONINTERACTIVE" != true ]; then
  read -rp "One-line purpose: " PROJECT_PURPOSE
fi
if [ -z "$STACK" ] && [ "$NONINTERACTIVE" != true ]; then
  read -rp "Primary stack (web/python/android/node/dotnet-wpf/multi/none): " STACK
fi
STACK="${STACK:-none}"
case "$STACK" in
  web|python|android|node|dotnet-wpf|multi|none) ;;
  *)
    echo "Invalid stack '$STACK'; defaulting to none (keep all examples)."
    STACK=none
    ;;
esac
if [ -z "$INTERVAL" ] && [ "$NONINTERACTIVE" != true ]; then
  read -rp "Template update check interval (off/daily/weekly/monthly/on_session) [weekly]: " INTERVAL
fi
INTERVAL="${INTERVAL:-weekly}"

if [ "$REFERENCE" != true ] && [ -n "$STACK" ] && [ -n "$PROJECT_PURPOSE" ]; then
  $PY - "$STACK" "$PROJECT_PURPOSE" "$ROOT" << 'PY'
import sys
from pathlib import Path

stack, purpose, root = sys.argv[1], sys.argv[2], Path(sys.argv[3])
replacements = [
    ("[INSERT PLATFORM / TECH STACK HERE]", stack),
    ("[INSERT DETAILED APP DESCRIPTION AND GOALS HERE]", purpose),
]
for rel in ("docs/INITIALIZATION_PROMPT.md", "AGENT_MEMORY.md"):
    path = root / rel
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for old, new in replacements:
        text = text.replace(old, new)
    path.write_text(text, encoding="utf-8")
PY
fi

if [ "$REFERENCE" != true ]; then
  $PY - "$INTERVAL" "$ROOT/.template-update.json" << 'PY'
import json, sys
interval, path = sys.argv[1], sys.argv[2]
with open(path, encoding="utf-8") as f:
    d = json.load(f)
d["check_interval"] = interval
with open(path, "w", encoding="utf-8") as f:
    json.dump(d, f, indent=2)
    f.write("\n")
PY
fi

if [ -z "$RELEASE_REPO" ] && [ "$NONINTERACTIVE" != true ] && [ "$REFERENCE" != true ]; then
  read -rp "GitHub owner/repo for app release checks (OWNER/REPO) [skip]: " RELEASE_REPO
fi
if [ -z "$DONATION_URL" ] && [ "$NONINTERACTIVE" != true ] && [ "$REFERENCE" != true ]; then
  read -rp "Donation URL [skip]: " DONATION_URL
fi

$PY - "$ROOT" "${RELEASE_REPO:-}" "${DONATION_URL:-}" << 'PY'
import json, shutil, sys
from pathlib import Path
root, repo, url = sys.argv[1], sys.argv[2], sys.argv[3]
root = Path(root)
src_app = root / ".app-update.json.example"
dst_app = root / ".app-update.json"
if src_app.exists() and not dst_app.exists():
    shutil.copy(src_app, dst_app)
if repo.strip() and dst_app.exists():
    data = json.loads(dst_app.read_text(encoding="utf-8"))
    data["release_repo"] = repo.strip()
    dst_app.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")
src_don = root / "donations.json.example"
dst_don = root / "donations.json"
if src_don.exists() and not dst_don.exists():
    shutil.copy(src_don, dst_don)
if url.strip() and dst_don.exists():
    data = json.loads(dst_don.read_text(encoding="utf-8"))
    data["links"] = [{"label": "Donate", "url": url.strip()}]
    dst_don.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")
PY

$PY scripts/sync-stack-config.py "$ROOT" "${RELEASE_REPO:-}" "${DONATION_URL:-}"

if [ "$REFERENCE" != true ]; then
  if [ -z "$CODEOWNER" ] && [ "$NONINTERACTIVE" != true ]; then
    read -rp "GitHub username for CODEOWNERS (without @): " CODEOWNER
  fi
  if [ -n "$CODEOWNER" ]; then
    sed -i "s/@\[PROJECT_OWNER\]/@$CODEOWNER/g" .github/CODEOWNERS 2>/dev/null || \
      sed -i '' "s/@\[PROJECT_OWNER\]/@$CODEOWNER/g" .github/CODEOWNERS
  fi

  ABOUT="$PROJECT_NAME - $PROJECT_PURPOSE. Built with agent-project-bootstrap. FOSS MIT."
  $PY - "$ABOUT" "$ROOT/docs/GITHUB_ABOUT.md" << 'PY'
import sys
from pathlib import Path
about, path = sys.argv[1], Path(sys.argv[2])
path.write_text(
    f"""# GitHub About Block

## Draft Description (edit to <=350 chars)

{about}

## Topics

Add topics relevant to your project and stack.
""",
    encoding="utf-8",
)
PY
fi

PRUNED=false
if [ "$REFERENCE" = true ] || [ "$PRUNE_FLAG" = "no" ]; then
  echo "Skipping prune (reference mode or --no-prune)."
elif [ "$STACK" = "none" ]; then
  echo "Stack 'none': keeping all examples and modules."
elif [ "$STACK" = "multi" ]; then
  if [ "$PRUNE_FLAG" = "yes" ]; then
    echo "Keeping all examples (multi-stack)."
  elif [ "$NONINTERACTIVE" = true ]; then
    echo "Skipping prune (--non-interactive)."
  else
    read -rp "Prune unused examples/modules? (y/N): " PRUNE
    if [ "$PRUNE" = "y" ] || [ "$PRUNE" = "Y" ]; then
      echo "Keeping all examples (multi-stack)."
    fi
  fi
else
  if [ "$PRUNE_FLAG" = "yes" ]; then
    PRUNED=true
    prune_primary_stack "$STACK"
  elif [ "$NONINTERACTIVE" = true ]; then
    echo "Skipping prune (--non-interactive)."
  else
    read -rp "Prune unused examples/modules? (y/N): " PRUNE
    if [ "$PRUNE" = "y" ] || [ "$PRUNE" = "Y" ]; then
      PRUNED=true
      prune_primary_stack "$STACK"
    fi
  fi
fi

$PY scripts/init-stack-sync.py "$STACK" "$ROOT" "$PRUNED"
if [ -f scripts/sync-design-tokens.py ]; then
  $PY scripts/sync-design-tokens.py || true
fi
echo "Wrote .cursor/stack-selection.json and synced AGENT_MEMORY active modules."

echo ""
echo "=== Workflow validation ==="
if [ -f scripts/validate-workflow-actions.sh ] && command -v gh >/dev/null 2>&1; then
  if bash scripts/validate-workflow-actions.sh; then
    echo "Workflow action refs validated via GitHub API."
  else
    echo "WARN: validate-workflow-actions.sh failed. Fix refs before first push."
  fi
elif [ ! -f scripts/validate-workflow-actions.sh ]; then
  echo "SKIP: validate-workflow-actions.sh not yet migrated (Phase 3)."
else
  echo "WARN: gh CLI not found. Install GitHub CLI and run:"
  echo "  bash scripts/validate-workflow-actions.sh"
fi

echo ""
echo "=== Done ==="
echo ""
echo "Stack selection: .cursor/stack-selection.json"
if [ "$REFERENCE" = true ]; then
  echo "Reference mode complete — customized docs preserved."
else
  echo "Next: Read @docs/START_HERE.md and @docs/CURSOR_MODES.md (after Phase 2)."
fi
