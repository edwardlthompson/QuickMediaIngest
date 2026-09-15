"""LX-L2: Linux adapters (XDG, 0600 FTP secrets, trash, uid 0, path watch, xdg-open)."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
APP = ROOT / "QuickMediaIngest"


def main() -> int:
    uid = (APP / "Core/UidGuard.cs").read_text(encoding="utf-8")
    assert "getuid" in uid
    assert "TryRefuseRoot" in uid
    secrets = (APP / "Core/Services/FileFtpCredentialStore.cs").read_text(encoding="utf-8")
    assert "UnixFileMode.UserRead" in secrets and "ftp-secrets" in secrets
    libsecret = (APP / "Core/Services/SecretServiceFtpCredentialStore.cs").read_text(encoding="utf-8")
    assert "secret-tool" in libsecret and "FileFtpCredentialStore" in libsecret
    watch = (APP / "Core/PathWatchTimings.cs").read_text(encoding="utf-8")
    assert "500" in watch and "3000" in watch
    trash = (APP / "Core/GioTrashService.cs").read_text(encoding="utf-8")
    assert "ITrashService" in trash and "gio" in trash
    xdg = (APP / "Core/XdgAppPaths.cs").read_text(encoding="utf-8")
    assert "XDG_CONFIG_HOME" in xdg
    shell = (APP / "Core/ShellOpen.cs").read_text(encoding="utf-8")
    assert "xdg-open" in shell
    assert "ArgumentList" in shell
    luks = (APP / "Core/Services/DestinationEncryptionDetector.cs").read_text(encoding="utf-8")
    assert "LuksEncrypted" in luks and "crypto_LUKS" in luks
    core = (ROOT / "QuickMediaIngest.Core/QuickMediaIngest.Core.csproj").read_text(encoding="utf-8")
    assert "NetVips.Native.linux-x64" in core
    print("OK: Linux adapters (XDG, 0600 secrets, trash, uid0, watch, xdg-open, linux-x64 vips)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
