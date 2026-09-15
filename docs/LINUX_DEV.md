# Linux development

This product is a **.NET 8 WPF** app. Native Windows CI runs the full `QuickMediaIngest-1.sln` (`net8.0-windows` STA tests). This host is Linux Mint: WPF tests cannot run here even after the SDK is installed.

## .NET 8 SDK (no sudo)

```bash
bash scripts/install-dotnet-sdk-linux.sh
```

Installs channel **8.0** to `$HOME/.dotnet`. `feature-gate.sh` looks at `DOTNET_ROOT`, `$HOME/.dotnet/dotnet`, `PATH`, then WSL `dotnet.exe` before treating the SDK as missing.

Optional system-wide packages (sudo) are not required; user-local install is canonical.

## Linux `/build` and feature-gate

On non-Windows, `feature-gate.sh --stack dotnet-wpf` does **not** environment-block the whole stack:

1. Hygiene, encoding, file-limits (always)
2. Skip `net8.0-windows` restore/build/test (Windows CI)
3. If `QuickMediaIngest.Core.Tests` exists **and** a SDK is found, `dotnet test` that `net8.0` project
4. License check

`QuickMediaIngest.Desktop` is the Avalonia ingest-bench (`net8.0`). Linux feature-gate builds it after Core.Tests. Do not flip `.cursor/stack-selection.json` to `multi`.

`scripts/pack-deb.sh` publishes **trimmed** linux-x64 ReadyToRun and runs `QuickMediaIngest.Desktop --smoke-native` (Magick + libvips) before building the `.deb`. SQLite stays Windows WPF-only. Native AOT is still out of v1.

## Why WPF `dotnet test` stays on Windows

`QuickMediaIngest.Tests` targets `net8.0-windows`. The TFM needs Windows Presentation Foundation and an STA thread. Linux can host the SDK and later Core.Tests; it cannot host WPF STA.

## Android on Linux (KVM, udev, plugdev)

This child does not ship Android. If you use the template emulator docs locally:

1. Add your user to `plugdev` and `kvm` as needed
2. Install a udev rule so `adb` works without `sudo`, then:

```bash
sudo udevadm control --reload-rules && sudo udevadm trigger
```

Accept the RSA prompt on the device the first time `adb devices` lists it as `unauthorized`.
