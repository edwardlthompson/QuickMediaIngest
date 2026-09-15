"""WPF-child HUMAN leftovers: security policy, theme QA, SDK path, .deb smoke/sign."""
from __future__ import annotations

import os
from pathlib import Path

from human_task_core import AttemptResult, run_cmd
from security_scan_policy import check_repo as check_security
from theme_qa import check_repo as check_theme


def automate_t10_security(root: Path, _cfg: dict) -> AttemptResult:
    errors = check_security(root)
    if errors:
        return AttemptResult(1, "t10-security", "; ".join(errors), True)
    return AttemptResult(0, "t10-security", "Trivy required; gitleaks/semgrep advisory", False)


def automate_theme_qa(root: Path, _cfg: dict) -> AttemptResult:
    errors = check_theme(root)
    if errors:
        return AttemptResult(1, "theme-qa", "; ".join(errors), True)
    return AttemptResult(0, "theme-qa", "THEME_QA tokens and chrome checks passed", False)


def automate_sdk_sys(root: Path, _cfg: dict) -> AttemptResult:
    _ = root
    home = Path(os.environ.get("HOME", "") or Path.home()) / ".dotnet" / "dotnet"
    return AttemptResult(
        0,
        "sdk-sys",
        f"canonical Linux SDK is user-local $HOME/.dotnet (no apt/sudo); binary {home}",
        False,
    )


def automate_mint_deb_smoke(root: Path, _cfg: dict) -> AttemptResult:
    code, out = run_cmd(root, ["bash", str(root / "scripts/smoke-deb.sh")])
    if code != 0:
        return AttemptResult(1, "deb-smoke", out or "smoke-deb.sh failed", True)
    return AttemptResult(0, "deb-smoke", "smoke-deb.sh passed", False)


def automate_deb_sign(root: Path, _cfg: dict) -> AttemptResult:
    script = root / "scripts/sign-deb.sh"
    if not script.is_file():
        return AttemptResult(1, "deb-sign", "scripts/sign-deb.sh missing", True)
    code, out = run_cmd(root, ["bash", str(script)])
    if code != 0:
        return AttemptResult(1, "deb-sign", out or "sign-deb.sh failed", True)
    return AttemptResult(0, "deb-sign", "v1 unsigned .deb policy encoded", False)
