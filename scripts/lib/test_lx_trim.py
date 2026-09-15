"""LX-trim: PublishTrimmed after Magick/NetVips native smoke (SQLite is WPF-only)."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def main() -> int:
    smoke = (ROOT / "QuickMediaIngest/Core/NativeLibrarySmoke.cs").read_text(encoding="utf-8")
    assert "ProbeMagick" in smoke
    assert "ProbeVips" in smoke
    assert "SKIP sqlite" in smoke
    program = (ROOT / "QuickMediaIngest.Desktop/Program.cs").read_text(encoding="utf-8")
    assert "NativeLibrarySmoke.TryHandle" in program
    desk = (ROOT / "QuickMediaIngest.Desktop/QuickMediaIngest.Desktop.csproj").read_text(
        encoding="utf-8"
    )
    assert "Magick.NET-Q16-AnyCPU" in desk
    assert "NetVips" in desk
    assert "TrimmerRootAssembly" in desk
    assert "QuickMediaIngest.Core" in desk
    pack = (ROOT / "scripts/pack-deb.sh").read_text(encoding="utf-8")
    assert "PublishTrimmed=true" in pack
    assert "--smoke-native" in pack
    tests = (ROOT / "QuickMediaIngest.Core.Tests/NativeLibrarySmokeTests.cs").read_text(
        encoding="utf-8"
    )
    assert "TryHandle_SmokeNative_LoadsMagickAndVips" in tests
    print("OK: LX-trim Magick/NetVips smoke + trimmed pack-deb (SQLite SKIP on Desktop)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
