"""LX-L1: Core + Localization are net8.0; Windows adapters live outside Core."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def _tfm(csproj: Path) -> str:
    text = csproj.read_text(encoding="utf-8")
    assert "<TargetFramework>net8.0</TargetFramework>" in text, csproj
    assert "net8.0-windows" not in text, csproj
    return text


def main() -> int:
    core = _tfm(ROOT / "QuickMediaIngest.Core/QuickMediaIngest.Core.csproj")
    assert "QuickMediaIngest" in core and "Core" in core
    assert "**" in core and ".cs" in core
    _tfm(ROOT / "QuickMediaIngest.Localization/QuickMediaIngest.Localization.csproj")
    _tfm(ROOT / "QuickMediaIngest.Core.Tests/QuickMediaIngest.Core.Tests.csproj")
    wpf = (ROOT / "QuickMediaIngest/QuickMediaIngest.csproj").read_text(encoding="utf-8")
    assert "QuickMediaIngest.Core.csproj" in wpf, wpf
    assert "QuickMediaIngest.Localization.csproj" in wpf, wpf
    assert r'Compile Remove="Core\**\*.cs"' in wpf or "Compile Remove=\"Core" in wpf, wpf
    adapter = ROOT / "QuickMediaIngest/Platform/Windows"
    for name in (
        "WindowsAppPaths.cs",
        "WindowsFtpCredentialStore.cs",
        "SystemThemeDetector.cs",
        "AccessibilityPreferencesDetector.cs",
        "DeviceWatcher.cs",
        "RemovableDriveIo.cs",
    ):
        assert (adapter / name).is_file(), name
    assert not (ROOT / "QuickMediaIngest/Core/DeviceWatcher.cs").exists()
    loc = ROOT / "QuickMediaIngest.Localization"
    assert (loc / "AppLocalizer.cs").is_file()
    assert (ROOT / "QuickMediaIngest/Localization/Loc.cs").is_file()
    print("OK: Core + Localization net8.0; Windows adapters; Loc MarkupExtension stays in WPF")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
