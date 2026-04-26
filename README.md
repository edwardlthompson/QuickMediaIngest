# ⚡ Quick Media Ingest

**Quick Media Ingest** is a lightning-fast, open-source media importer built for photographers and videographers. It is designed to rapidly ingest photos and videos from SD cards, local drives, and **Android FTP Servers** into structured, customized folder hierarchies.

Inspired by the workflow efficiency of classic Windows Essentials Photo Gallery, but modernized with **Android Material Design**, **Dark Mode support**, and advanced **Whitelisting/Blacklisting** controls.

---


## 🚀 Key Features

* **⚡ High-Speed Ingestion**: Multi-threaded asynchronous copying with hash/size verification and safe collision suffixing.
* **📂 Unified Scanner (Local + FTP)**: Select SD/local/FTP directly, or use `Unified (SD + FTP)` to merge all active sources into one timeline.
* **🔄 One-Click Source Refresh**: Top-bar `Refresh` performs a full rescan and can force thumbnail cache rebuild when refreshing sources.
* **🧭 Verbose Scan Progress Overlay**: Real-time folder/file progress with current folder, per-folder counts, total counts, and scan phase details.
* **🖼️ Auto Preview Loading (Photos + Videos)**: Local, FTP, and Unified previews load automatically during scan without a separate manual preview pass.
* **📷 RAW-Friendly Preview Strategy**: RAW files prefer companion rendered previews (`.jpg/.jpeg/.heic/.heif`) when available for visual parity; shell fallback remains for codec-backed formats.
* **🗂️ Preview Stacks + Compare Mode**: RAW+rendered pairs can stay stacked by default, or be expanded side-by-side for quick visual comparison.
* **🎚️ Adjustable Preview Throughput**: Thumbnail Performance modes `Low`, `Balanced`, `Max`, and `Ultra` tune local and FTP thumbnail worker counts.
* **🧹 Dynamic Shoot Grouping**: Grouping by adjustable time gap (hours) with fast regrouping from cached scan data.
* **📝 Shoot-Level Editing**: Rename shoot titles inline and use shoot name tokens in filename templates.
* **🧠 Advanced Filename Templates**: Date/time (including milliseconds), sequence token support, separator controls, lowercase option, live preview examples, and stronger millisecond fallback when source metadata is second-only.
* **🔐 Delete-After-Import Safety**: Warning prompt appears once, remembers acknowledgement/cancel, and persists state.
* **🧪 Preflight + Reports**: Run import preflight checks and export import result artifacts (`json` + `txt`) for auditing.
* **📋 Queue, Retry, and Resume**: Queue import jobs, retry failed files, and resume pending import plans after interruption.
* **🧷 Duplicate + Verification Policies**: Choose duplicate handling (`Suffix`, `Skip`, `OverwriteIfNewer`) and verification mode (`Fast` size-check or `Strict` hash verification).
* **🎛️ Presets + Live Filters**: Save/load workflow presets and filter preview surfaces by keyword/type while reviewing.
* **💾 Persistent App State**: Remembers theme, settings menu expansion, window size/position/state, thumbnail zoom, naming options, and FTP reconnect preferences.
* **🧾 Build Metadata in About**: About dialog surfaces app version and build date from the deployed executable.
* **📝 In-App Changelog + Release Links**: About dialog includes one-click access to GitHub changelog and release pages.
* **🌐 FTP Reconnect on Startup**: Attempts non-blocking reconnect to the last FTP source and reports unobtrusively if unavailable.
* **⬆️ Flexible Updates**: In-app updater supports both **Portable (.exe)** and **Installer (.msi)** channels with selectable check intervals, explicit selected-asset status, and restart-safe external handoff to avoid in-use install failures.

## 🛠️ Usage

1. **Connect Sources**: Insert SD/local media and/or add an FTP source.
2. **Pick View**: Select a single source or `Unified (SD + FTP)` for a merged import surface.
3. **Scan + Preview**: Watch detailed scan progress while shoots and previews populate automatically.
4. **Review Shoots**: Rename shoots, review source folders/date ranges, and select shoots/items.
5. **Preflight/Queue (Optional)**: Run `Preflight`, queue a batch with `Queue`, or resume pending jobs.
6. **Import**: Use `Import` and monitor verbose overall + per-group progress, ETA, speed, and failed-item list.
7. **Retry/Report**: Retry failed files quickly and review generated import reports in destination `_ImportReports`.
8. **Refresh When Needed**: Use top-bar `Refresh` to force full source/preview refresh and cache rebuild.

---

## ⚙️ Settings Surface

The sidebar `Settings` expander includes:

* **Preferences**
* **Add FTP Source**
* **Import History**
* **About & Updates**

Preferences currently include destination path, naming template controls, thumbnail performance mode, stack expansion mode, duplicate policy, verification mode, and preset save/load controls.

---

## 🛠️ Build & Development

Built using **.NET 8 (C#)** and **WPF**.

To build a **Single-File Portable Executable** locally:

```bash
build_local_test.bat
```

This creates a local test build at:
`publish/local-test/QuickMediaIngest.exe`

### ✅ Automatic Local Test Build

The local test app is now built automatically when running:

* `build_and_push.bat`
* `create_release_tag.bat`

### 🔢 Automatic Versioning

`build_and_push.bat` now auto-selects a Semantic Version bump using release heuristics:

* **Major**: explicit breaking-change markers (e.g., `BREAKING CHANGE`, `feat!:`).
* **Minor**: feature-level changes (e.g., `feat:` commits or newly added app-surface files).
* **Patch**: fixes, maintenance, or docs/tooling changes.

If needed, you can override the automatic choice by setting `VERSION_BUMP=patch`, `VERSION_BUMP=minor`, or `VERSION_BUMP=major` before running the script.

### 🤖 CI/CD (GitHub Actions)

Pushes to `main` now automatically:

* Build portable EXE + MSI.
* Check the version in the project file.
* Create and push tag `v<Version>` if it does not already exist.
* Publish a GitHub Release with build artifacts.

This prevents the "version bumped but no GitHub Release appeared" issue.

---

## 🤝 Contributing

Contributions are welcome! Please feel free to open Issues or submit Pull Requests for visual/feature enhancements.

## 📄 License

This project is free and open source. A formal license file is not currently included.
