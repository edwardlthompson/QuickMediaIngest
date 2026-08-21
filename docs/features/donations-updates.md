# Donations and filename-version updates

Quiet Venmo support and a non-blocking GitHub update check that compares product versions in release asset filenames.

## Acceptance

- About includes **Donate via Venmo** (`https://venmo.com/code?user_id=1857304970395648420`). The update/install overlay does not.
- First run records the installed version and does not show a donate popup.
- After the installed version changes, one optional overlay appears: **Development is still going** with **Donate via Venmo** | **Not now**. Either button records seen-for-this-version.
- Background update check runs at most once per 24 hours (Off still skips network). User-Agent `QuickMediaIngest-Updater`, 10s timeout.
- Newer matching versioned asset (`QuickMediaIngest-X.Y.Z-x64.exe` portable, `QuickMediaIngest-X.Y.Z-x64-setup.msi` installer) prompts **Install** | **Later**. Later silences that product version. Failures stay silent.
- Donate prefs and last-check timestamps live in `%AppData%\QuickMediaIngest\update-donate.json` only.

## Smoke

1. Fresh AppData: launch once. No donate popup. `update-donate.json` has `recordedInstalledVersion`.
2. Edit `recordedInstalledVersion` to a previous value, relaunch. Donate overlay appears once; **Not now** does not show it again.
3. About → Donate via Venmo opens the public Venmo URL.
4. About → Check now against a release that only has unversioned `QuickMediaIngest.exe`: stay silent / no update.
5. After a release that includes `QuickMediaIngest-X.Y.Z-x64.exe` newer than the running build: **Install** opens the asset URL; **Later** does not ask again for that version.

## Critique

| Issue | Resolution |
|----|---|
| Null/empty at boundary | Parser returns null; empty assets stay silent (`UpdateDonateTests`) |
| Network timeout | 10s CTS on the GitHub GET; exceptions return default |
| Race | Donate and update overlays are mutually exclusive; donate shows first |
| Unhandled exceptions | Fetch, parse, and store I/O wrapped; startup check is fire-and-forget |
