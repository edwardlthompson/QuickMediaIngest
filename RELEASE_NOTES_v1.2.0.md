# Release notes — Quick Media Ingest **v1.2.0** (2026-04-27)

## Highlights

This release improves **reliability and clarity** during scans, brings a **consistent modal experience**, adds **localized UI strings** (English, Spanish, French), and refines **sidebar layout**—especially for **Unified** imports and **scan exclusions**.

---

## Reliability & workflow

### Unified source scan

After refreshing **Unified (SD + FTP)**, the scan progress overlay now **closes as soon as sources are merged and groups are rebuilt**. Preview thumbnail generation continues in the background without blocking the overlay. This removes the confusing “stuck blurred screen” effect when preview work runs longer than the merge phase.

### Scan summary after import lists

Scan results sometimes reported paths as if they were **FTP errors**, when those paths were intentionally **hidden by your Scan exclusions**. The summary now **separates**:

- **Folders omitted by design** (your exclusion rules — expected behavior).
- **FTP folders that failed to list** (connection/server issues).

You can optionally **suppress the summary dialog** when the only omissions are due to exclusions (preference is saved). Status bar messaging matches the scenario.

---

## UI / UX

### Modal overlays (blur & dim)

Preferences and Scan exclusions panels now use the **same blurred backdrop pattern** as other dialogs (FTP, drives, About, progress). Backdrop layers render at the window root so the VisualBrush blur matches consistently.

Backdrop drawing uses edge-to-edge fill and tint for fewer visible seams at window borders.

### Escape / Cancel behavior

Escape follows a predictable **overlay stack**: scan exclusions → preferences → scan progress → drive picker → FTP dialog → About → skipped-folder summary.

### Sidebar & theme

- **Theme** control sits **directly under the sidebar collapse toggle** (expanded layout).
- When the sidebar is **collapsed**, the labeled theme row hides; only the **icon** on the collapsed rail is shown (no duplicate wide control).

---

## Localization

User-visible strings use the **resource-based localization** pipeline with **English** defaults and satellite assemblies for **Spanish** and **French**. Display language can be chosen in Preferences (restart may apply some UI chrome changes).

---

## Settings & exclusions

The **Scan exclusions** surface (drives removed from scans, folder blacklist) stays accessible from the sidebar with the same polished overlay treatment as Preferences.

---

## Developers & operators

- **CHANGELOG.md** carries the authoritative version-to-version history.
- Version is defined in **`QuickMediaIngest.csproj`** (`<Version>`).

---

### Upgrade notes

No database or config schema migration required for typical installs. Existing `config.json` gains optional keys when new preferences are saved (defaults preserve prior behavior).

Portable and MSI update channels behave as in v1.1.x; use **About → Check for updates** after publish.
