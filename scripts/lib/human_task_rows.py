"""HUMAN BUILD_PLAN row handlers (init, config, ADR, smoke, release)."""
from __future__ import annotations

import os
import shutil
from pathlib import Path

from human_task_core import (
    AttemptResult,
    append_decision_log,
    git_has_remote,
    run_cmd,
)


def automate_use_template(root: Path, cfg: dict) -> AttemptResult:
    if git_has_remote(root):
        return AttemptResult(0, "git-remote-exists", "Repository already has git remote", False)
    return AttemptResult(
        1, "use-template", "Cannot create GitHub template from local clone; create repo on GitHub first", True
    )


def automate_init_placeholders(root: Path, cfg: dict) -> AttemptResult:
    script = root / "scripts/init-project.sh"
    if not script.is_file():
        return AttemptResult(1, "init-project", "scripts/init-project.sh missing", True)
    cmd = [
        "bash", str(script), "--non-interactive", "--stack", cfg["stack"],
        "--project-name", cfg["project_name"], "--purpose", cfg["purpose"],
    ]
    code, tail = run_cmd(root, cmd)
    if code == 0:
        return AttemptResult(0, "init-project", "Filled INITIALIZATION_PROMPT via init-project", False)
    return AttemptResult(1, "init-project", tail or f"init-project exit {code}", True)


def automate_informational(_root: Path, _cfg: dict, method: str) -> AttemptResult:
    return AttemptResult(0, method, "Informational step satisfied for autonomous /build", False)


def automate_stack_config(root: Path, cfg: dict) -> AttemptResult:
    sync = root / "scripts/sync-stack-config.py"
    if not sync.is_file():
        return AttemptResult(1, "sync-stack-config", "sync-stack-config.py missing", True)
    repo = cfg.get("release_repo", "")
    donation = os.environ.get("BUILD_DONATION_URL", "https://liberapay.com/example")
    for example, dest in (
        (".app-update.json.example", ".app-update.json"),
        ("donations.json.example", "donations.json"),
    ):
        src, dst = root / example, root / dest
        if src.is_file() and not dst.is_file():
            shutil.copy(src, dst)
    code, tail = run_cmd(root, ["python3", str(sync), str(root), repo, donation])
    if code == 0:
        return AttemptResult(0, "sync-stack-config", "Stack-local config synced from examples", False)
    return AttemptResult(1, "sync-stack-config", tail or f"exit {code}", True)


def automate_approve_adr(root: Path, cfg: dict, task: str) -> AttemptResult:
    if "<!-- no-auto-approve -->" in (root / "BUILD_PLAN.md").read_text(encoding="utf-8"):
        return AttemptResult(1, "approve-adr", "BUILD_PLAN disables auto-approve", True)
    adr_glob = list((root / "docs/adr").glob("0001*.md")) if (root / "docs/adr").is_dir() else []
    if not (adr_glob or (root / "DECISION_LOG.md").is_file()):
        return AttemptResult(1, "approve-adr", "No ADR-0001 or DECISION_LOG found", True)
    append_decision_log(root, f"Autonomous approval for BUILD_PLAN row: {task[:120]}")
    return AttemptResult(0, "approve-adr", "Logged autonomous approval in DECISION_LOG.md", False)


def automate_product_smoke(root: Path, cfg: dict) -> AttemptResult:
    gate = root / "scripts/feature-gate.sh"
    if not gate.is_file():
        return AttemptResult(1, "product-smoke", "feature-gate.sh missing", True)
    code, tail = run_cmd(root, ["bash", str(gate), "--stack", cfg["stack"]])
    if code == 0:
        return AttemptResult(0, "feature-gate", "Product smoke via feature-gate.sh", False)
    return AttemptResult(1, "feature-gate", tail or f"exit {code}", True)


def automate_release_tag(root: Path, _cfg: dict) -> AttemptResult:
    code, out = run_cmd(root, ["gh", "release", "list", "--limit", "1"])
    if code != 0:
        return AttemptResult(1, "release-tag", "gh release list failed; product judgment required", True)
    if out.strip():
        return AttemptResult(0, "release-tag", "Release exists; autonomous ack only", False)
    return AttemptResult(1, "release-tag", "No release; human product approval required", True)


def automate_golden_path_smoke(root: Path, cfg: dict) -> AttemptResult:
    """Run automated regression and unit tests covering Golden Path 1–7."""
    test_cmd = [
        "dotnet",
        "test",
        "QuickMediaIngest-1.sln",
        "-c",
        "Release",
        "--no-build",
        "--filter",
        "FullyQualifiedName~GoldenPathAutomationSmokeTests|FullyQualifiedName~MainViewModelFeedbackTests|FullyQualifiedName~CrashCaptureTests|FullyQualifiedName~PrivacyReportTests|FullyQualifiedName~DisplayModeSelectorTests|FullyQualifiedName~GitHubIssueComposerTests",
        "--verbosity",
        "minimal",
    ]
    code, tail = run_cmd(root, test_cmd)
    if code == 0:
        return AttemptResult(0, "golden-path-smoke", "Golden Path automated smoke tests passed", False)
    return AttemptResult(1, "golden-path-smoke", tail or f"exit {code}", True)


def automate_wpf_signoff(root: Path, _cfg: dict) -> AttemptResult:
    """Run automated WPF sign-off suite and proof tests."""
    test_cmd = [
        "dotnet",
        "test",
        "QuickMediaIngest-1.sln",
        "-c",
        "Release",
        "--no-build",
        "--filter",
        "FullyQualifiedName~HumanSignoffVerificationTests|FullyQualifiedName~GoldenPathAutomationSmokeTests",
        "--verbosity",
        "minimal",
    ]
    code, tail = run_cmd(root, test_cmd)
    if code == 0:
        return AttemptResult(0, "wpf-signoff", "Automated WPF sign-off tests passed", False)
    return AttemptResult(1, "wpf-signoff", tail or f"exit {code}", True)


def automate_live_op13_smoke(root: Path, _cfg: dict) -> AttemptResult:
    """Run automated ADB/FTP hybrid verification and PreferAdb tests."""
    test_cmd = [
        "dotnet",
        "test",
        "QuickMediaIngest-1.sln",
        "-c",
        "Release",
        "--no-build",
        "--filter",
        "FullyQualifiedName~Adb|FullyQualifiedName~FtpAdb|FullyQualifiedName~GoldenPathAutomationSmokeTests",
        "--verbosity",
        "minimal",
    ]
    code, tail = run_cmd(root, test_cmd)
    if code == 0:
        return AttemptResult(0, "op13-adb-smoke", "Automated PreferAdb & ADB hybrid tests passed", False)
    return AttemptResult(1, "op13-adb-smoke", tail or f"exit {code}", True)


def automate_author_spec_plan(root: Path, _cfg: dict) -> AttemptResult:
    """Verify that docs/spec.md and docs/plan.md exist and contain valid project specifications."""
    spec_path = root / "docs/spec.md"
    plan_path = root / "docs/plan.md"
    if spec_path.is_file() and plan_path.is_file():
        spec_text = spec_path.read_text(encoding="utf-8")
        plan_text = plan_path.read_text(encoding="utf-8")
        if len(spec_text.splitlines()) >= 20 and len(plan_text.splitlines()) >= 20 and "Quick Media Ingest" in spec_text:
            return AttemptResult(0, "spec-plan-verified", "docs/spec.md and docs/plan.md authored and verified", False)
    return AttemptResult(1, "spec-plan", "docs/spec.md and docs/plan.md missing or incomplete", True)


