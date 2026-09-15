# Quick Media Ingest

Ingest photos and videos into dated, shoot-based folders.

## Pitch

FOSS .NET 8 desktop ingest for photographers: SD cards, local drives, and FTP into dated shoot-based folders. Windows ships WPF; Linux ships an Avalonia ingest-bench `.deb`.

## Features

- SD card, local drive, and FTP ingest with Unified source view
- Dated shoot-based folders with configurable file naming
- Duplicate policies, verification, and delete-after-import safety
- WPF on Windows and Avalonia Fluent ingest-bench on Linux
- Portable EXE, WiX MSI, and linux-x64 `.deb` via GitHub Releases

## Quick start

1. Clone the repo and open `QuickMediaIngest-1.sln` or run `dotnet build`.
2. Read `docs/START_HERE.md`. First-time walk: `docs/help/TOUR.md`.
3. Linux: `bash scripts/install-dotnet-sdk-linux.sh` then feature-gate `dotnet-wpf`.

## For humans

Windows: portable EXE and WiX MSI. Linux: install `quick-media-ingest` to `/opt/quick-media-ingest`.

## For agents

Read `docs/START_HERE.md`, `docs/CURSOR_MODES.md`, and `AGENTS.md`. Stack is `dotnet-wpf` only.

## Install

GitHub Releases. Linux `.deb` publishes a self-contained linux-x64 ReadyToRun binary.

## Usage

Use `/verify` before merge and `/ship` to publish. Do not run `init-project.sh` or prune on this child product.

## Contributing

See `CONTRIBUTING.md`.

## Security

See `SECURITY.md` and `docs/SECURITY_TRIAGE.md`.

## License

MIT License — see `LICENSE`.
