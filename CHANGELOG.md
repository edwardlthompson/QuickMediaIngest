# Changelog

## Unreleased

- Import History: add confirmation dialog before clearing history and improve CSV export (headers + proper CSV escaping, UTF-8)
- Preview: preserve RAW shell preview aspect ratio to prevent incorrect thumbnail dimensions
- Preview: include video thumbnails in local, FTP, and unified preview loading paths
- Import workflow: add preflight report, queueing, retry failed files, and resume pending import plan support
- Ingest options: add duplicate handling policies (`Suffix`, `Skip`, `OverwriteIfNewer`) and verification modes (`Fast`, `Strict`)
- Naming reliability: improve millisecond handling via EXIF subsecond parsing and non-zero fallback when metadata precision is second-level
- UX/settings: add stack compare toggle, preset save/load, and top-bar filtering controls
- Updates: switch to external updater handoff that waits for app exit before applying MSI/EXE updates, preventing in-use install failures
- About dialog: add reliable build date display from assembly timestamp and direct links to changelog/release pages
- FTP video previews: improve thumbnail extraction reliability with longer video download timeout and shell preview fallback for codec-specific videos
