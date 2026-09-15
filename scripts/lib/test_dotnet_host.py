"""UNB-PATH/GATE: resolve user-local SDK and skip WPF STA off Windows."""
from __future__ import annotations

import os
from pathlib import Path

from dotnet_host import core_tests_project, find_dotnet

ROOT = Path(__file__).resolve().parents[2]


def main() -> int:
    os.environ.pop("DOTNET_ROOT", None)
    fake = Path("/tmp/qmi-dotnet-root")
    fake.mkdir(parents=True, exist_ok=True)
    bin_path = fake / "dotnet"
    bin_path.write_text("#!/bin/sh\n", encoding="utf-8")
    bin_path.chmod(0o755)
    os.environ["DOTNET_ROOT"] = str(fake)
    assert find_dotnet() == str(bin_path), find_dotnet()
    assert core_tests_project(ROOT) is None
    print("OK: dotnet_host DOTNET_ROOT + no Core.Tests yet")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
