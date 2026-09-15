"""T10: Trivy is the required Security Scan bar; gitleaks/semgrep stay advisory."""
from __future__ import annotations

import re
from pathlib import Path

SECURITY = Path(".github/workflows/security.yml")
JOB = re.compile(r"^  ([a-z][a-z0-9_-]*):\n(.*?)(?=^  [a-z]|\Z)", re.M | re.S)


def job_blocks(text: str) -> dict[str, str]:
    marker = "\njobs:\n"
    idx = text.find(marker)
    blob = text[idx + len(marker) :] if idx >= 0 else text
    return {name: body for name, body in JOB.findall(blob)}


def check_repo(root: Path) -> list[str]:
    path = root / SECURITY
    if not path.is_file():
        return [f"MISSING: {SECURITY.as_posix()}"]
    jobs = job_blocks(path.read_text(encoding="utf-8"))
    errors: list[str] = []
    trivy = jobs.get("trivy", "")
    if not trivy:
        errors.append("security.yml must define a trivy job")
    elif "continue-on-error: true" in trivy:
        errors.append("Trivy must remain a required Security Scan job")
    elif 'exit-code: "1"' not in trivy and "exit-code: '1'" not in trivy:
        errors.append("Trivy must fail the workflow on CRITICAL/HIGH findings")
    for name in ("gitleaks", "semgrep"):
        body = jobs.get(name, "")
        if not body:
            errors.append(f"security.yml must define a {name} job")
        elif "continue-on-error: true" not in body:
            errors.append(f"{name} stays advisory (continue-on-error) until a tracked follow-up")
    if not (root / "scripts/check-gitleaks-baseline.sh").is_file():
        errors.append("missing scripts/check-gitleaks-baseline.sh")
    if not (root / "scripts/check-semgrep.sh").is_file():
        errors.append("missing scripts/check-semgrep.sh")
    return errors


def main() -> int:
    errors = check_repo(Path.cwd())
    if errors:
        print("Security scan policy check failed:")
        for item in errors:
            print(f"  {item}")
        return 1
    print("OK: Trivy required; gitleaks/semgrep advisory; local config gates present")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
