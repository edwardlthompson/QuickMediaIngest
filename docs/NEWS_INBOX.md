# News Inbox

Auto-collected inbox of news, upstream updates, security fixes, and ideas relevant to this repo, appended daily by News Bot.
A Cursor planning agent should review these items and fold the useful ones into BUILD_PLAN; entries are append-only, newest at the bottom.


## 2026-11-13

**News and updates**

- .NET November servicing (10.0.15 / 9.0.23 / 8.0.34) fixes ~25 CVEs; CVE-2026-118747 also needs a System.Formats.Cbor NuGet update. [source](https://devblogs.microsoft.com/dotnet/dotnet-and-dotnet-framework-november-2026-servicing-updates/)
- .NET 11 is GA, including WPF/WinForms/Windows Desktop. [source](https://devblogs.microsoft.com/dotnet/announcing-dotnet-11/)
- Lightroom Classic 16.0: import filter bar, Vectorscope, AF-point overlays; drops macOS 14 support. [source](https://helpx.adobe.com/lightroom-classic/desktop/introduction-to-lightroom-classic/release-notes.html)

**Tasks for the planning agent**

- [ ] Rebuild on .NET 8.0.34 SDK/runtime — Ships the November security fixes to users of the WPF app. Label: `[AGENT]` · Priority: P1 · https://devblogs.microsoft.com/dotnet/dotnet-and-dotnet-framework-november-2026-servicing-updates/
- [ ] Plan a .NET 11 target migration — A newer runtime keeps the app on a supported, current line. Label: `[AGENT]` · Priority: P3 · https://devblogs.microsoft.com/dotnet/announcing-dotnet-11/
- [ ] Borrow LrC 16 import filters (file type, rating, label) — Feature ideas for faster, filtered ingest. Label: `[AGENT]` · Priority: P3 · https://helpx.adobe.com/lightroom-classic/desktop/introduction-to-lightroom-classic/release-notes.html
