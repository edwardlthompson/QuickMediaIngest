"""LX-L4: debian packaging + pack-deb.sh (self-contained R2R, AppStream, desktop)."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def main() -> int:
    script = (ROOT / "scripts/pack-deb.sh").read_text(encoding="utf-8")
    assert "PublishReadyToRun=true" in script
    assert "PublishTrimmed=true" in script
    assert "--smoke-native" in script
    assert "PublishSingleFile=false" in script
    assert "linux-x64" in script
    assert "lintian --fail-on error" in script
    assert "lintian-overrides" in script
    overrides = (ROOT / "packaging/debian/lintian-overrides").read_text(encoding="utf-8")
    assert "dir-or-file-in-opt" in overrides
    desktop = (ROOT / "packaging/debian/quick-media-ingest.desktop").read_text(encoding="utf-8")
    assert "Exec=/opt/quick-media-ingest/QuickMediaIngest.Desktop" in desktop
    assert "Icon=quick-media-ingest" in desktop
    assert "StartupWMClass=quick-media-ingest" in desktop
    assert "hicolor" in script
    assert "quick-media-ingest.png" in script
    meta = (ROOT / "packaging/debian/io.github.edwardlthompson.QuickMediaIngest.metainfo.xml").read_text(encoding="utf-8")
    assert "desktop-application" in meta
    control = (ROOT / "packaging/debian/control").read_text(encoding="utf-8")
    assert "Package: quick-media-ingest" in control
    branding = (ROOT / "branding/product.json").read_text(encoding="utf-8")
    assert "avalonia-linux" in branding
    agent = (ROOT / "AGENT.md").read_text(encoding="utf-8")
    assert "QuickMediaIngest.Desktop" in agent
    spec = (ROOT / "docs/spec.md").read_text(encoding="utf-8")
    plan = (ROOT / "docs/plan.md").read_text(encoding="utf-8")
    assert "Avalonia ingest-bench" not in spec
    assert "pack-deb" not in plan
    print("OK: pack-deb.sh + debian AppStream/.desktop (Sacred spec/plan untouched)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
