# ⚡ Quick Media Ingest

**Quick Media Ingest** is a lightning-fast, open-source media importer built for photographers and videographers. It is designed to rapidly ingest photos and videos from SD cards, local drives, and **Android FTP Servers** into structured, customized folder hierarchies.

Inspired by the workflow efficiency of classic Windows Essentials Photo Gallery, but modernized with **Android Material Design**, **Dark Mode support**, and advanced **Whitelisting/Blacklisting** controls.

---

## 🚀 Key Features

*   **⚡ High-Speed Ingestion**: Multi-threaded asynchronous copying with automatic hash verification to ensure data integrity and avoid duplicates.
*   **📂 Unified Scanner (Local & FTP)**: Scan any local drive, SD card, or network folder. Built-in high-performance **FTPS Support** for importing directly from Android over Wifi/Ethernet.
*   **📷 RAW file support**: Super-fast visual preview grid using **Embedded JPEG Extraction** from popular raw image formats (CR2, NEF, DNG).
*   **🛡️ Smart Whitelisting (Persistent IDs)**: Mark specific paths (e.g., `/DCIM`) per device to avoid scanning WhatsApp or system folders. Uses `.importer-id` files in the root for persistent identification.
*   **🔄 Conflict Handling**: Automatically appends sequential numerics (`_01`, `_02`) if collisions occur.
*   **🧹 Dynamic Organizing**: Group items into structured albums with adjustable times and user formats (e.g., `YYYY-MM-DD-HH-MM-SS+Album`).

---

## 🛠️ Usage

1.  **Insert Device**: Plug in your SD card or connect your Phone's FTP.
2.  **Visual Preview**: View the populated grid of groupings divided by shoot time.
3.  **Set Album Name**: Name the batch, adjust the Date/Time headers if grouping into a range.
4.  **Import**: Click `Import` and watch it work.

---

## 🛠️ Build & Development

Built using **.NET 8 (C#)** and **WPF**. 

To build a **Single-File Portable Executable** locally:
```bash
dotnet publish QuickMediaIngest\QuickMediaIngest.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o ./publish/portable
```

### 🤖 CI/CD (GitHub Actions)
Fully automated builds are configured to pack a Portable EXE and an ZIP package automatically on Tag Releases (`v*`).

---

## 🤝 Contributing
Contributions are welcome! Please feel free to open Issues or submit Pull Requests for visual/feature enhancements.

## 📄 License
Released under the [MIT License](LICENSE).
