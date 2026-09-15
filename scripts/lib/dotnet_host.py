"""Locate a .NET SDK and decide whether this host can run WPF STA tests."""
from __future__ import annotations

import os
import sys
from pathlib import Path

CORE_TESTS = (
    "QuickMediaIngest.Core.Tests/QuickMediaIngest.Core.Tests.csproj",
    "QuickMediaIngest.Core/QuickMediaIngest.Core.Tests.csproj",
)


def is_windows_host() -> bool:
    if sys.platform == "win32":
        return True
    osname = os.environ.get("OS", "")
    return osname.startswith("Windows") or "MINGW" in osname or "MSYS" in osname


def find_dotnet(home: Path | None = None) -> str | None:
    home = home or Path(os.environ.get("HOME") or Path.home())
    names = ("dotnet", "dotnet.exe")
    ordered: list[Path] = []
    root = os.environ.get("DOTNET_ROOT", "").strip()
    if root:
        ordered.extend(Path(root) / name for name in names)
    ordered.extend(home / ".dotnet" / name for name in names)
    path_env = os.environ.get("PATH", "")
    for folder in path_env.split(os.pathsep):
        if not folder:
            continue
        ordered.extend(Path(folder) / name for name in names)
    ordered.append(Path("/mnt/c/Program Files/dotnet/dotnet.exe"))
    seen: set[str] = set()
    for path in ordered:
        key = str(path)
        if key in seen:
            continue
        seen.add(key)
        if path.is_file() and os.access(path, os.X_OK):
            return str(path)
        if path.suffix.lower() == ".exe" and path.is_file():
            return str(path)
    return None


def core_tests_project(root: Path) -> str | None:
    for rel in CORE_TESTS:
        path = root / rel
        if path.is_file():
            return str(path)
    return None


def main(argv: list[str] | None = None) -> int:
    args = list(sys.argv[1:] if argv is None else argv)
    root = Path.cwd()
    if "--print" in args:
        found = find_dotnet()
        if found:
            print(found)
        return 0
    if "--host" in args:
        print("windows" if is_windows_host() else "linux")
        return 0
    if "--core-tests" in args:
        found = core_tests_project(root)
        if found:
            print(found)
        return 0
    print("Usage: dotnet_host.py --print|--host|--core-tests", file=sys.stderr)
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
