# ⚡ Quick Media Ingest

**Quick Media Ingest** is a lightning-fast, open-source media importer built for photographers and videographers. It is designed to rapidly ingest photos and videos from SD cards, local drives, and **Android FTP Servers** into structured, customized folder hierarchies.

Inspired by the workflow efficiency of classic Windows Essentials Photo Gallery, but modernized with **Android Material Design**, **Dark Mode support**, and advanced **Whitelisting/Blacklisting** controls.

---


## 🚀 Key Features

* **⚡ High-Speed Ingestion**: Multi-threaded asynchronous copying with hash/size verification and safe collision suffixing.
* **📂 Unified Scanner (Local + FTP)**: Select SD/local/FTP directly, or use `Unified (SD + FTP)` to merge all active sources into one timeline.
* **🔄 One-Click Source Refresh**: Top-bar `Refresh` performs a full rescan and can force thumbnail cache rebuild when refreshing sources.
* **🧭 Verbose Scan Progress Overlay**: Real-time folder/file progress with current folder, per-folder counts, total counts, and scan phase details.
* **🖼️ Auto Preview Loading**: Local, FTP, and Unified previews load automatically during scan without a separate manual preview pass.
* **📷 RAW-Friendly Preview Strategy**: RAW files prefer companion rendered previews (`.jpg/.jpeg/.heic/.heif`) when available for visual parity; shell fallback remains for codec-backed formats.
* **🗂️ Preview Stacks**: RAW+rendered pairs can be shown as a single visible preview tile in the shoot grid while preserving import selection behavior.
* **🎚️ Adjustable Preview Throughput**: Thumbnail Performance modes `Low`, `Balanced`, `Max`, and `Ultra` tune local and FTP thumbnail worker counts.
* **🧹 Dynamic Shoot Grouping**: Grouping by adjustable time gap (hours) with fast regrouping from cached scan data.
* **📝 Shoot-Level Editing**: Rename shoot titles inline and use shoot name tokens in filename templates.
* **🧠 Advanced Filename Templates**: Date/time (including milliseconds), sequence token support, separator controls, lowercase option, and live preview examples.
* **🔐 Delete-After-Import Safety**: Warning prompt appears once, remembers acknowledgement/cancel, and persists state.
* **💾 Persistent App State**: Remembers theme, settings menu expansion, window size/position/state, thumbnail zoom, naming options, and FTP reconnect preferences.
* **🌐 FTP Reconnect on Startup**: Attempts non-blocking reconnect to the last FTP source and reports unobtrusively if unavailable.
* **⬆️ Flexible Updates**: In-app updater supports both **Portable (.exe)** and **Installer (.msi)** channels with selectable check intervals and explicit selected-asset status.

## 🛠️ Usage

1. **Connect Sources**: Insert SD/local media and/or add an FTP source.
2. **Pick View**: Select a single source or `Unified (SD + FTP)` for a merged import surface.
3. **Scan + Preview**: Watch detailed scan progress while shoots and previews populate automatically.
4. **Review Shoots**: Rename shoots, review source folders/date ranges, and select shoots/items.
5. **Import**: Use the top-bar `Import` button and monitor overall + per-group progress.
6. **Refresh When Needed**: Use top-bar `Refresh` to force full source/preview refresh and cache rebuild.

---

## ⚙️ Settings Surface

The sidebar `Settings` expander includes:

* **Preferences**
* **Add FTP Source**
* **Import History**
* **About & Updates**

Preferences currently include destination path, naming template controls, and thumbnail performance mode.

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
