# AGENT.md — original product brief (Sacred)

Quick Media Ingest product brief. Bootstrap stamps `AGENTS.md` only and must not overwrite this file. Later sessions: read this before any BUILD_PLAN sprint row. Do not substitute template About/donate examples for this product.

<!-- agent-brief:one-liner -->
Ingest photos and videos into dated, shoot-based folders
<!-- /agent-brief:one-liner -->

<!-- agent-brief:keywords -->
ingest, photos, videos, wpf, foss
<!-- /agent-brief:keywords -->

## Rules

- FOSS .NET 8 WPF desktop app for photographers and videographers.
- Import from SD cards, local drives, and FTP into dated shoot-based destination folders.
- Configurable naming, metadata options, and safety checks.
- Linux ingest-bench head: `QuickMediaIngest.Desktop` (Avalonia). Parity gap list and BUILD_PLAN LP rows: `docs/features/linux-parity.md`. `.cursor/stack-selection.json` stays `dotnet-wpf`.
- Do not run `init-project.sh` or prune on this child. Stack is `dotnet-wpf` only.
- Never overwrite `AGENTS.md`, `docs/spec.md`, `docs/plan.md`, or `QuickMediaIngest/` from template stubs.

## First milestone

Template catch-up to agent-project-bootstrap v1.5.0 (Canon + Mixed). Golden Path product slices already shipped in WPF.
