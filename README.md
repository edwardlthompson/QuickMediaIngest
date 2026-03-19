# ⚡ Quick Media Ingest

**Quick Media Ingest** is a lightning-fast, open-source media importer built for photographers and videographers. It is designed to rapidly ingest photos and videos from SD cards, local drives, and **Android FTP Servers** into structured, customized folder hierarchies.

Inspired by the workflow efficiency of classic Windows Essentials Photo Gallery, but modernized with **Android Material Design**, **Dark Mode support**, and advanced **Whitelisting/Blacklisting** controls.

---

## 🚀 Key Features

* **⚡ High-Speed Ingestion**: Multi-threaded asynchronous copying with automatic hash verification to ensure data integrity and avoid duplicates.
* **📂 Unified Scanner (Local & FTP)**: Scan any local drive, SD card, or network folder. Built-in high-performance **FTPS Support** for importing directly from Android over Wifi/Ethernet.
* **📷 RAW file support**: Super-fast visual preview grid using **Embedded JPEG Extraction** from popular raw image formats (CR2, NEF, DNG).
* **🛡️ Smart Whitelisting (Persistent IDs)**: Mark specific paths (e.g., `/DCIM`) per device to avoid scanning WhatsApp or system folders. Uses `.importer-id` files in the root for persistent identification.
* **🔄 Conflict Handling**: Automatically appends sequential numerics (`_01`, `_02`) if collisions occur.
* **🧹 Dynamic Organizing**: Group items into structured albums with adjustable times and user formats (e.g., `YYYY-MM-DD-HH-MM-SS+Album`).

### Latest Updates (v1.0.4)

* Import progress dialog was decluttered by removing duplicate counters while keeping key progress, ETA, and data-rate info.
* Release process expectation tightened: each shipped update should include a local build and a GitHub build/release run.

### Previous Updates (v1.0.3)

* FTP scanner and folder discovery were rewritten for better reliability with Android FTP servers.
* Preview generation is now deferred and can be run only for selected shoots.
* FTP preview building now uses per-file timeout protection to prevent hangs.
* Import progress now shows overall imported count, current-shoot progress, elapsed time, ETA, and failed-file count.
* FTP import throughput improved by reusing a single FTP connection during import.
* Delete After Import now works for FTP sources (when server permissions allow delete).
* Import history is now saved (latest 50 records) with date/time, counts, and duration.
* Added quick shoot selection controls: Select All Shoots, All, and None.

---

## 🛠️ Usage

1. **Insert Device**: Plug in your SD card or connect your Phone's FTP.
2. **Scan Source**: Run a scan for local or FTP media.
3. **Select Shoots**: Use checkboxes or All/None to pick what you want.
4. **Build Selected Previews (Optional)**: Generate previews only for selected shoots.
5. **Import**: Click `Import` and monitor overall + per-shoot progress.

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
