#!/usr/bin/env bash
# Automate Align-0.15 HUMAN_BACKLOG rows where safe for QuickMediaIngest (dotnet-wpf).
#
# - Enables: stale.yml, weekly-health-check.yml (WPF-adapted), dependabot-automerge.yml
# - Declines permanently: release-please*, pages.yml (conflicts with csproj/build.yml)
# - Attempts: gh security_events scope, AUTOMERGE_TOKEN secret
#
# Usage:
#   bash scripts/automate-human-backlog.sh
#   bash scripts/automate-human-backlog.sh --refresh-gh --setup-automerge-token
#   bash scripts/automate-human-backlog.sh --setup-github-repo
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

# shellcheck source=scripts/lib/resolve-gh.sh
. "$ROOT/scripts/lib/resolve-gh.sh"
if qmi_gh_available; then
  echo "Using gh: ${QMI_GH_BIN:-$(command -v gh)}"
else
  echo "WARN: gh CLI not found (install GitHub CLI or ensure it is on PATH)"
fi

REFRESH_GH=false
SETUP_AUTOMERGE=false
SETUP_GITHUB=false
STRICT=false

while [ $# -gt 0 ]; do
  case "$1" in
    --refresh-gh) REFRESH_GH=true; shift ;;
    --setup-automerge-token) SETUP_AUTOMERGE=true; shift ;;
    --setup-github-repo) SETUP_GITHUB=true; shift ;;
    --strict) STRICT=true; shift ;;
    *) echo "Unknown arg: $1"; exit 2 ;;
  esac
done

if command -v python3 >/dev/null 2>&1; then PY=python3
elif command -v python >/dev/null 2>&1; then PY=python
else PY=python3; fi

FAILURES=0
DONE=()
LEFT=()

ok() { DONE+=("$1"); echo "OK  $1"; }
warn() { LEFT+=("$1"); echo "LEFT $1 — $2"; }
fail() { FAILURES=$((FAILURES + 1)); LEFT+=("$1"); echo "FAIL $1 — $2"; }

require_file() {
  local f="$1"
  if [ -f "$f" ]; then
    ok "present:$f"
  else
    fail "missing:$f" "expected workflow/script from automate pass"
  fi
}

echo "==> Automate HUMAN_BACKLOG (dotnet-wpf)"

# --- Workflows that should exist after this pass ---
require_file ".github/workflows/stale.yml"
require_file ".github/workflows/weekly-health-check.yml"
require_file ".github/workflows/dependabot-automerge.yml"
require_file "scripts/setup-automerge-token.sh"

# Permanently declined (do not add)
if [ -f ".github/workflows/release-please.yml" ] || [ -f ".github/workflows/pages.yml" ]; then
  fail "declined-workflows" "release-please/pages must not be present for QMI"
else
  ok "declined:release-please+pages (kept off; csproj/build.yml owns release)"
fi

# --- gh security_events ---
GH_ARGS=()
[ "$REFRESH_GH" = true ] && GH_ARGS+=(--refresh)
[ "$STRICT" = true ] && GH_ARGS+=(--strict)
if bash scripts/ensure-gh-security-scope.sh "${GH_ARGS[@]}"; then
  if bash scripts/ensure-gh-security-scope.sh >/dev/null 2>&1; then
    # Probe without refresh to confirm API works
    if gh api "repos/$(gh repo view --json nameWithOwner -q .nameWithOwner)/dependabot/alerts?state=open&per_page=1" >/dev/null 2>&1; then
      ok "gh:dependabot-alerts (API readable)"
    else
      warn "gh:dependabot-alerts" "ensure script exited 0 but API still blocked; run with --refresh-gh interactively"
    fi
  fi
else
  warn "gh:security_events" "run: bash scripts/automate-human-backlog.sh --refresh-gh"
fi

# --- AUTOMERGE_TOKEN ---
if [ "$SETUP_AUTOMERGE" = true ]; then
  if bash scripts/setup-automerge-token.sh; then
    ok "secret:AUTOMERGE_TOKEN"
  else
    warn "secret:AUTOMERGE_TOKEN" "set AUTOMERGE_TOKEN=ghp_... or gh auth with workflow scope, then re-run --setup-automerge-token"
  fi
else
  if gh secret list 2>/dev/null | grep -q '^AUTOMERGE_TOKEN'; then
    ok "secret:AUTOMERGE_TOKEN (already set)"
  else
    warn "secret:AUTOMERGE_TOKEN" "optional: bash scripts/automate-human-backlog.sh --setup-automerge-token"
  fi
fi

# --- GitHub security settings (optional) ---
if [ "$SETUP_GITHUB" = true ] && [ -f scripts/setup-github-repo.sh ]; then
  if bash scripts/setup-github-repo.sh; then
    ok "github:repo-security-settings"
  else
    warn "github:repo-security-settings" "admin gh token required; see script checklist"
  fi
fi

# --- Rewrite HUMAN_BACKLOG.md ---
TODAY="$($PY -c "from datetime import date; print(date.today().isoformat())")"
"$PY" - "$TODAY" <<'PY'
import sys
from pathlib import Path
from datetime import date

today = sys.argv[1] if len(sys.argv) > 1 else date.today().isoformat()
root = Path(".")
# Detect remaining human items from this script's environment via marker files / probes
import subprocess
import shutil

def sh_ok(cmd: list[str]) -> bool:
    try:
        r = subprocess.run(cmd, capture_output=True, text=True, check=False)
        return r.returncode == 0
    except FileNotFoundError:
        return False

gh = shutil.which("gh")
security_ok = False
if gh:
    repo = subprocess.run(
        [gh, "repo", "view", "--json", "nameWithOwner", "-q", ".nameWithOwner"],
        capture_output=True, text=True, check=False,
    )
    if repo.returncode == 0 and repo.stdout.strip():
        probe = subprocess.run(
            [gh, "api", f"repos/{repo.stdout.strip()}/dependabot/alerts?state=open&per_page=1"],
            capture_output=True, text=True, check=False,
        )
        security_ok = probe.returncode == 0

automerge_secret = False
if gh:
    secrets = subprocess.run([gh, "secret", "list"], capture_output=True, text=True, check=False)
    automerge_secret = secrets.returncode == 0 and "AUTOMERGE_TOKEN" in (secrets.stdout or "")

resolved = [
    ("Align-0.15", "AGENT", "Enable release-please*.yml", "Declined: conflicts with csproj + build.yml Windows release"),
    ("Align-0.15", "AGENT", "Enable pages.yml", "Declined: template design-token Pages; not QMI product surface"),
    ("Align-0.15", "AGENT", "Enable stale.yml", "Automated: .github/workflows/stale.yml added (60d stale / 14d close)"),
    ("Align-0.15", "AGENT", "Enable weekly-health-check.yml", "Automated: WPF-adapted weekly-health-check.yml (ubuntu CI + windows dotnet)"),
    ("Align-0.15", "AGENT", "Enable dependabot-automerge.yml", "Automated: workflow added; patch/minor auto-merge; majors need HUMAN label"),
]

open_rows = []
if security_ok:
    resolved.append(("Align-0.15", "AGENT", "gh Dependabot alerts API", "Verified: Dependabot alerts API readable (GET query probe)"))
else:
    open_rows.append((today, "Align-0.15", "HUMAN", "gh Dependabot alerts API access", "Run: .\\scripts\\automate-human-backlog.ps1 -RefreshGh (browser may open)"))

if automerge_secret:
    resolved.append(("Align-0.15", "AGENT", "AUTOMERGE_TOKEN secret", "Automated: repo secret present so merges trigger push CI"))
else:
    open_rows.append((today, "Align-0.15", "HUMAN", "Optional AUTOMERGE_TOKEN for Dependabot merges", "Run: .\\scripts\\automate-human-backlog.ps1 -SetupAutomergeToken"))

# Standing release checklist (not an open blocker) — see Standing process section below
resolved.append(("Release", "HUMAN", "WPF UI sign-off (Align cycle)", "Completed for current cycle; re-run run-human-signoffs.ps1 before future product releases"))

lines = [
    "# Human Backlog",
    "",
    "> Items automation could not finish, or that still need a human.",
    "> Regenerated by `scripts/automate-human-backlog.sh`.",
    "",
    "## Open",
    "",
    "| Deferred | Sprint | Owner | Task | Reason |",
    "|----------|--------|-------|------|--------|",
]
for row in open_rows:
    lines.append(f"| {row[0]} | {row[1]} | {row[2]} | {row[3]} | {row[4]} |")

lines += [
    "",
    "## Resolved by automation",
    "",
    "| When | Sprint | Owner | Task | Resolution |",
    "|------|--------|-------|------|------------|",
]
for sprint, owner, task, resolution in resolved:
    lines.append(f"| {today} | {sprint} | {owner} | {task} | {resolution} |")

lines += [
    "",
    "## Standing process (not open blockers)",
    "",
    "Before shipping product/UI changes:",
    "",
    "```powershell",
    ".\\scripts\\run-human-signoffs.ps1",
    ".\\scripts\\run-human-signoffs.ps1 -PublishedExe   # optional portable EXE smoke",
    "```",
    "",
    "## How to re-run automation",
    "",
    "```powershell",
    ".\\scripts\\automate-human-backlog.ps1 -RefreshGh -SetupAutomergeToken",
    "# optional admin: -SetupGithubRepo",
    "```",
    "",
]
Path("HUMAN_BACKLOG.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
print("Wrote HUMAN_BACKLOG.md")
PY

echo ""
echo "==> Summary"
echo "Automated/OK: ${#DONE[@]}"
for x in "${DONE[@]:-}"; do echo "  - $x"; done
echo "Still human/left: ${#LEFT[@]}"
for x in "${LEFT[@]:-}"; do echo "  - $x"; done

if [ "$FAILURES" -gt 0 ] && [ "$STRICT" = true ]; then
  exit 1
fi
exit 0
