"""Policy checks that replace former HUMAN T10 / THEME_QA rows."""
from __future__ import annotations

from pathlib import Path

from security_scan_policy import check_repo as check_security
from theme_qa import check_repo as check_theme

ROOT = Path(__file__).resolve().parents[2]


def main() -> int:
    sec = check_security(ROOT)
    theme = check_theme(ROOT)
    assert not sec, sec
    assert not theme, theme
    print("OK: security scan policy + THEME_QA")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
