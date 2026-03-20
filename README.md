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

### Latest Updates (v1.0.21)

* Unified the ribbon visual language across sidebar actions, top command groups, bottom option groups, and status band.
* Added a lightweight ribbon tab-strip shell (`Home`, `File`, `Edit`, `Find`, `Create`, `View`) to progress toward the template look.
* Added missing `Zoom` group caption so all bottom option groups now share the same visual structure.

### Previous Updates (v1.0.20)

* Refined the ribbon-style look to more closely match the template screenshot with clearer command-group framing.
* Added stronger visual group separations and group captions in both the top action ribbon and bottom options ribbon.
* Kept the existing wrap behavior and must-stick pairs while improving overall ribbon readability and structure.

### Previous Updates (v1.0.19)

* Started the first incremental ribbon-style pass in the top action bar.
* Added distinct bordered command groups (Import, FTP, Local) to create clearer visual separation between related actions.
* Added subtle group labels and retained wrapping behavior and must-stick control pairs.

### Previous Updates (v1.0.18)

* Locked the zoom controls as a single non-breaking unit so the magnifier and slider always stay together when wrapping.

### Previous Updates (v1.0.17)

* Improved top control bar wrapping behavior so actions wrap more reliably on narrower window widths.
* Grouped **Hours Between Shoots** label and textbox into one non-breaking unit so they no longer separate across lines.

### Previous Updates (v1.0.16)

* Fixed accent consistency: dark theme now uses yellow action text while light theme uses blue action text.
* Unified left sidebar action button colors so **Add FTP Source** and **Settings & About** match.
* Applied theme-aware accent coloring to the **Sources** refresh icon.
* Normalized bottom bar typography so **Hours Between Shoots** matches nearby control text size.
* Removed duplicate number display by dropping the hint overlay from the hours textbox.
* Aligned magnifier icon and zoom slider in a shared centered container.

### Previous Updates (v1.0.15)

* Corrected the header toggle semantics so the dark-state pill aligns with the moon side and the light-state pill aligns with the sun side.
* Improved top-bar alignment so the Scan button sits properly with Delete After Import.
* Reworked bar readability in both themes by removing hardcoded yellow text from functional controls and using theme-aware foregrounds.
* Shifted the Material Design accent toward blue for better light-mode contrast and a safer path toward the Excel 2003 ribbon styling.

### Previous Updates (v1.0.14)

* Restored the main window to a stable layout after the v1.0.12-v1.0.13 visual rewrite broke the visible controls.
* Replaced the fragile custom theme resource wiring with Material Design's native theme engine for dark/light switching.
* Added a working theme toggle in the upper-right header and kept Windows dark/light detection on startup.
* Split the lower area into a light controls bar and a separate status/progress bar for clearer readability.

### Previous Updates (v1.0.13)

**🔧 Theme System Critical Fixes:**
* **Fixed broken UI rendering** - Corrected resource dictionary loading order so Material Design loads first, then custom theme overlays on top.
* **Fixed resource path URIs** - Changed from relative paths to full `pack://application:,,,/` URIs for reliable resource resolution.
* **Added DarkTheme.xaml & LightTheme.xaml** - Separate theme variant files for active color overrides at runtime.
* **Implemented Windows System Theme Detection** - Auto-detects dark/light mode from Windows registry on startup (HKCU\...\Themes\Personalize\AppsUseLightTheme).
* **Binding system ready** - Theme toggle now functionally bound to `IsDarkTheme` ViewModel property with live theme switching.
* **App.xaml.cs theme switching** - Static method `App.ApplyTheme(useLightTheme)` enables runtime theme dictionary swapping.
* **Fixed nullable reference warnings** - Added proper nullability annotations for Registry access.

### Previous Updates (v1.0.12)
* **Comprehensive Theme Framework**: Created modular theme system with colors (dark/light), brushes, typography, and control styles in `Themes/` directory for future extensibility.
* **Excel 2003-Inspired Aesthetic**: Applied iconic cobalt blue (#007ACC) accent colors, light gray bar backgrounds, and horizontal divider lines between sections for retro-meets-modern visual hierarchy.
* **Block-Based Control Layout**: Wrapped each control group (Import, Scan, Delete, Browse, FTP Preview, Select All, Hours, Zoom) in individual styled blocks for clarity and language-agnostic usability.
* **Navigation Bar**: Added top navigation bar with application branding and **Dark/Light Theme Toggle** (moon/sun icons) in upper right corner with visual pill-button toggle control.
* **Redesigned Status Bar**: Moved progress meter with percentage to the left side of the lower bar; verbose status message now in a prominent secondary row for improved readability.
* **Universal Iconography**: Added emojis and Material Design icons throughout (📥 Import, 🔍 Scan, 🗑️ Delete, 🎬 FTP Preview, ⏱️ Hours, etc.) to make UI intuitively understandable in any language.
* **Professional Styling**: Segoe UI typography system implemented globally (11pt headers, 10pt body, 9pt captions); consistent margin/padding; smooth hover/focus states; responsive narrow-width layout preservation.

### Previous Updates (v1.0.11)

* Tightened bottom bar spacing and reduced control footprint for better narrow-width usability.
* Reduced status/progress footprint in the bottom bar while preserving the same information.

### Previous Updates (v1.0.10)

* Replaced All/None buttons with a single **Select All** checkbox and positioned it next to **Include Subfolders** in the bottom bar.
* Moved **Hours Between Shoots** and thumbnail zoom controls to the bottom bar.
* Moved **Delete After Import** next to the Import button.
* Refined Sources header alignment (refresh icon aligned with Sources title) and matched sidebar button typography.

### Previous Updates (v1.0.9)

* Renamed preview action to **Build FTP Previews** and show it only for FTP sources (SD card/local previews continue to build automatically during scan).
* Local release process now explicitly refreshes `publish/local-test/QuickMediaIngest.exe` so the local EXE version tracks each release.

### Previous Updates (v1.0.8)

* Kept Import and Scan as one non-breaking action group in the top banner so they no longer wrap apart on narrow widths.
* Maintained two-row wrapped toolbar behavior for remaining controls.

### Previous Updates (v1.0.7)

* Top banner controls were split into two wrapped rows so priority actions (Import, Scan, Build Previews) stay in the first row.
* Improved narrow-window behavior so key controls remain discoverable while secondary controls wrap below.

### Previous Updates (v1.0.6)

* Removed the visible "Current Source Path" field from the top controls to reduce noise.
* Redesigned top tools into a single wrapped banner with a distinct divider line above the main content.
* Top controls now wrap instead of clipping/hiding when the window width is reduced.

### Previous Updates (v1.0.5)

* Fixed local single-file EXE startup crash caused by window icon path resolution during XAML load.
* Embedded the icon as a WPF resource so the published standalone executable launches correctly.

### Previous Updates (v1.0.4)

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
