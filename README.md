# ⚡ Quick Media Ingest

**Quick Media Ingest** is a lightning-fast, open-source media importer built for photographers and videographers. It is designed to rapidly ingest photos and videos from SD cards, local drives, and **Android FTP Servers** into structured, customized folder hierarchies.

Inspired by the workflow efficiency of classic Windows Essentials Photo Gallery, but modernized with **Android Material Design**, **Dark Mode support**, and advanced **Whitelisting/Blacklisting** controls.

---

## 🚀 Key Features

* **⚡ High-Speed Ingestion**: Multi-threaded asynchronous copying with automatic hash verification to ensure data integrity and avoid duplicates.
* **📂 Unified Scanner (Local & FTP)**: Selecting a source (SD/local/FTP) automatically scans and groups media.
* **🖼️ Auto Preview Loading**: Thumbnails are loaded automatically during scan (including FTP) with no manual preview pass required.
* **🚀 Faster SD Preview Pipeline**: Local preview loading now prioritizes embedded EXIF thumbnails and uses bounded parallel thumbnail generation for much faster SD-card ingest preparation.
* **📷 RAW/Modern Format Support**: Includes DNG, HEIC/HEIF, CR2/CR3, NEF, ARW, RAF, ORF, RW2, SRW plus common photo/video types.
* **🧠 Shell Thumbnail Fallback**: Uses Windows Shell extraction as a last resort for codec-backed formats.
* **🛡️ Smart Whitelisting (Persistent IDs)**: Mark specific paths (e.g., `/DCIM`) per device to avoid scanning WhatsApp or system folders. Uses `.importer-id` files in the root for persistent identification.
* **🔄 Conflict Handling**: Automatically appends sequential numerics (`_01`, `_02`) if collisions occur.
* **🧹 Dynamic Organizing**: Group items into structured albums with adjustable times and user formats (e.g., `YYYY-MM-DD-HH-MM-SS+Album`).
* **🧩 Draggable Ribbon Tiles**: Reorder Import controls via drag handle; order persists across launches.
* **🔎 Persistent Zoom**: Thumbnail zoom level is saved and restored between runs.
* **🪟 Remembered Window State**: The app restores window size and maximized state between launches.
* **⬆️ Flexible Updates**: In-app updater supports **Portable (.exe)** and **Installer (.msi)** channels with selectable check interval.

## 🛠️ Usage

1. **Insert Device**: Plug in your SD card or connect your Phone's FTP.
2. **Select Source**: Click your SD/local/FTP source; scan starts automatically.
3. **Select Shoots**: Use checkboxes or All/None to pick what you want.
4. **Import**: Click `Import` and monitor overall + per-shoot progress.
5. **Review Refresh**: After import, the source list refreshes automatically so deleted/new files are reflected.

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
