# Completed Tasks

> Archive of finished BUILD_PLAN items.

---

## Remembered window + preview pane (2026-09-15)

- Avalonia restores size/position/maximized and a splitter-resized right preview of the selected file (1600-edge cache)

## Pipelined import verify (2026-09-15)

- ✅ [AGENT] Pipelined import verify: copy/delete and SHA-256 catalog overlap; status shows Copy n/N and Verify m/N; files leave the shoot list as each dest hashes; no second dest-tree walk

## Linux UX polish (named UX) (2026-09-15)

- ✅ [AGENT] UX-onboard Linux welcome: bind `Onboarding_Body` + app icon; heading 20px (not 24); Got it dismisses to empty card
- ✅ [AGENT] UX-type-scale Enforce ingest-chrome type 12/14/16/20 on Desktop (no 24px headings)
- ✅ [AGENT] UX-copy-empty Empty CTA “Scan for cards”; sources “No cards yet”; rewrite `Toolbar_RefreshTooltip` (drop “unified”)
- ✅ [AGENT] UX-empty-icon Empty-state SD Path that reads as a card (or remove the fake rectangle)
- ✅ [AGENT] UX-disclose Hide Group-by hours + thumbnail sliders until `ShowShootList` (nothing to group/zoom)
- ✅ [AGENT] UX-view-label Rename command-bar `…` to **View** (`Toolbar_ViewSection` + tooltip)
- ✅ [AGENT] UX-ptp PTP label “USB camera (PTP)”; off first paint — View/overflow only
- ✅ [AGENT] UX-dup-ftp Single Add FTP on the empty card; do not also show it as a first-paint sidebar peer
- ✅ [AGENT] UX-sidebar Rail = Sources + Notifications + Settings expander (Preferences, History, exclusions, Feedback, Add FTP); Eject only when a mount exists; primary nav rows 44px if they stay primary
- ✅ [AGENT] UX-import-enable Disable Import until a shoot is selected; tooltip “Select a shoot to import.”
- ✅ [AGENT] UX-delete-copy Label “Delete originals after import”; danger outline `#F44336`; keep the existing confirm
- ✅ [AGENT] UX-progress-cancel Cancel on the import progress overlay → existing cancel-import confirm (Linux overlay has none today)
- ✅ [AGENT] UX-prefs-labels Label every Settings ComboBox (theme, preset, date/time/separator, dest preset, duplicate, verify, timezone)
- ✅ [AGENT] UX-prefs-json Rename Settings footer Import → “Import settings file” (does not collide with ingest Import)
- ✅ [AGENT] UX-prefs-321 Second-copy field label “Second copy folder” (not watermark “3-2-1 destination”)
- ✅ [AGENT] UX-prefs-expand Settings import block → Expander (match WPF Appearance / Naming / Import)
- ✅ [AGENT] UX-ftp-copy FTP failure copy: lead with “Can’t reach the camera folder. Try /DCIM.”; keep long body as secondary
- ✅ [AGENT] UX-filetype File-type combo uses display names (WPF `FilterFileTypeToDisplayConverter`), not raw `All`/`Images`/`Videos`
- ✅ [AGENT] UX-hit-test `IsHitTestVisible=True` on every overlay scrim (first-run and peers; crash/prompt already lock)
- ✅ [AGENT] UX-live Live region: `AutomationProperties.LiveSetting=Polite` + `A11y_StatusAnnouncements` (Desktop 1×1 Opacity-0 host is unnamed)
- ✅ [AGENT] UX-a11y-names `AutomationProperties.Name` on dest chip, filter-chip ✕ (`A11y_RemoveFilterChip`), sidebar rows, empty Scan/Refresh
- ✅ [AGENT] UX-muted Caption/muted 12px opacity ≥ 0.78 (AA); do not drop tertiary text further
- ✅ [AGENT] UX-aaa-focus Visible focus rings on Fluent dark chrome (AAA opportunity from the audit)
- ✅ [AGENT] UX-aaa-44 44px min-height on Dry run and Refresh (AAA; Import already 44×88)
- ✅ [AGENT] UX-motion-overlay Overlay enter opacity 0→1, 180ms cubic-out; honor `ReducedMotion` (Desktop currently discards the duration)
- ✅ [AGENT] UX-motion-press Import press scale 1→0.96→1, 90ms (Windows `MotionAssist`)
- ✅ [AGENT] UX-motion-chevron Shoot expand chevron rotate 0°→90°, 120ms
- ✅ [AGENT] UX-motion-rows Shoot-card fade-in 160ms, stagger 30ms/card, cap 5
- ✅ [AGENT] UX-motion-skel Thumbnail skeleton pulse 900ms while Magick runs, then swap
- ✅ [AGENT] UX-motion-afterglow Afterglow card rise 12px / 180ms
- ✅ [AGENT] UX-motion-chip Filter-chip remove collapse width+opacity, 140ms
- ✅ [AGENT] UX-motion-dest Dest-chip accent-border flash 220ms after folder pick
- ✅ [AGENT] UX-blur Wire `OverlayBlurRadius` on Linux scrims or delete the unused property
- ✅ [AGENT] UX-notify-count Notification unread count on the Notifications row (not a plain text button)
- ✅ [AGENT] UX-status Status line always shows last scan/import sentence (never a blank bar)
- ✅ [AGENT] UX-sources-rail After scan, source rows in the rail (path leaf + transport), like Windows Sources list
- ✅ [AGENT] UX-ftp-empty Bind FTP-failure empty panel on Linux (`Empty_FtpFailureTitle` / Body + Refresh / Add FTP / Close)
- ✅ [AGENT] UX-last-dest Empty state remembers last destination (Rapid Photo Downloader: looks ready to run)
- ✅ [AGENT] UX-three-zone When a card is mounted: rail sources, shoot grid, dest summary; command bar shrinks to Import + dest + delete-after (Lightroom Import three-column, Fluent)
- ✅ [AGENT] UX-ingest-sheet Dest chip opens dest+naming ingest sheet (Photo Mechanic ingest sheet; reuse `FileNamingBuilder`; not the full Settings wall)
- ✅ [HUMAN] Mint glance: first-run shows body + empty card; no sliders/PTP dump; Import disabled until a shoot is selected

## Linux ingest-bench depth (named LD) (2026-09-15)

- ✅ [AGENT] LD-naming File-naming builder: token chips `[Date]`/`[Original]`/…, presets, Date/Time/Sequence toggles, format/separator, live preview
- ✅ [AGENT] LD-shoot-card Shoot card: expand, editable title, keywords, folder path, start/end, file count, ignore-folder
- ✅ [AGENT] LD-thumbs-grid Per-shoot thumbnail wrap-grid + zoom (no WIC)
- ✅ [AGENT] LD-filters Group-by hours, file-type filter, keyword filter chips, expand-all
- ✅ [AGENT] LD-prefs-import Import Settings depth: dest presets, duplicate policy, verification, RAW+JPEG stack, confirm-before-import

## Linux ingest-bench parity (named LP) (2026-09-15)

- ✅ [AGENT] LP-host Desktop composition: `XdgAppPaths`, persist first-run, Avalonia `IUserPrompt` host, load crash store into AppModel; no `System.Windows` in Desktop
- ✅ [AGENT] LP-scan Local + `/media`/`/run/media` removable scan, Refresh, drive-select overlay, waiting-for-card empty state
- ✅ [AGENT] LP-dest Browse destination (Avalonia folder picker), naming template, dest chip
- ✅ [AGENT] LP-import Real Import + Dry run + delete-after + free-space + collisions + `gio trash` (both heads via Core)
- ✅ [AGENT] LP-shoots Shoot groups, select all, skip folder, keyword filter, transport badge
- ✅ [AGENT] LP-ftp Add/test/browse FTP + libsecret + bandwidth throttle overlay
- ✅ [AGENT] LP-adb Prefer-ADB when `adb` on PATH + dual-FTP alias de-dupe
- ✅ [AGENT] LP-thumbs Magick/Vips preview grid + thumbnail cache cap (no WIC)
- ✅ [AGENT] LP-scene Full-bleed import progress + afterglow + Open folder (`xdg-open`)
- ✅ [AGENT] LP-crash Crash overlay wired to pending-crash store + discarded fingerprints
- ✅ [AUTO] LP-v1-smoke `pack-deb.sh` + `--smoke-native` after Wave A
- ✅ [AGENT] LP-chrome Command bar overflow, notifications flyout, 14px/`#007ACC`, Import 44×88
- ✅ [AGENT] LP-history Import history search/filter/export CSV (replace stub overlay)
- ✅ [AGENT] LP-excl Scan exclusions editor (replace stub overlay)
- ✅ [AGENT] LP-feedback Feedback composer + GitHub duplicate search + Esc/Ctrl+Enter
- ✅ [AGENT] LP-prefs Preferences: theme, language, naming, GPS strip, settings search, JSON import/export
- ✅ [AGENT] LP-about About + donate + filename-version GitHub updates
- ✅ [AGENT] LP-prompt Non-destructive confirms via `IUserPrompt` (delete-after stays destructive)
- ✅ [AGENT] LP-queue Queue import, retry failed, resume pending plan
- ✅ [AGENT] LP-cull Pick/reject, stars, color labels, persist cull across rescan
- ✅ [AGENT] LP-post SHA-256 manifest, 3-2-1 second dest, XMP/copyright, already-imported hash catalog
- ✅ [AGENT] LP-watch Watch-folder, shoot split/merge, timezone, batch rename, dest template tokens
- ✅ [AGENT] LP-media HEIC via vips, video first-frame/proxy, optional ffmpeg, compare view, ICC, cache purge
- ✅ [AGENT] LP-wifi Camera Wi-Fi FTP folder presets (Sony/Canon/Nikon/Fuji/Panasonic)
- ✅ [AGENT] LP-ptp PTP/USB tether browse via libusb (not WPD)
- ✅ [AGENT] LP-a11y High-contrast, reduced motion, RTL `FlowDirection`, F1 shortcuts, live regions
- ✅ [AGENT] LP-eject Linux unmount (`gio mount -u` / udisks) + leftover-files reminder after delete-after
- ✅ [AUTO] LP-parity-pack `pack-deb.sh` + `--smoke-native` after Wave C
- ✅ [HUMAN] Mint visual glance of ingest-bench (Import/Dry run/empty/overlays; optional)

---

## Two heads — Windows WPF + Linux Mint .deb (named LX) (2026-09-14)

- ✅ [AGENT] LX-L1 Extract `QuickMediaIngest.Core` + `QuickMediaIngest.Localization` (`net8.0`); Windows adapters for WMI, Credential Manager, Registry theme, `IAppPaths`; Core.Tests run on Linux; WPF Windows CI still green
- ✅ [AGENT] LX-L1b Shared IngestBench AppModel (`net8.0`); `MainViewModel` forwards import/scan/empty-state/first-run/dry-run/delete-after; Desktop binds the same type; no second Import ViewModel
- ✅ [AGENT] LX-L2 Linux adapters: XDG paths, inotify+500ms debounce+3s poll, 0600 FTP secrets, `ITrashService` (`gio trash` then unlink), `xdg-open`, refuse uid 0, `NetVips.Native.linux-x64`
- ✅ [AGENT] LX-L3 Avalonia ingest-bench binds AppModel (Import / Dry run / Refresh, empty state, first-run, crash overlay; AXAML ≤800)
- ✅ [AGENT] LX-L4 `packaging/debian` + `scripts/pack-deb.sh` on ubuntu-latest only (self-contained untrimmed ReadyToRun multi-file); AppStream + `.desktop`; `lintian` errors fail; stamp AGENT.md / `branding/product.json` stacks — do not overwrite Sacred spec/plan
- ✅ [AUTO] LX-L5 `scripts/smoke-deb.sh` after L4 (dpkg-deb, lintian if present, refuse uid 0); `--card` lists mounts when present
- ✅ [AGENT] LX-libsecret FTP passwords via Secret Service (v1 ships 0600 file + mode test)
- ✅ [AGENT] LX-trim `PublishTrimmed` after Magick/SQLite/NetVips smoke
- ✅ [AGENT] LX-luks Destination encryption detector on Linux
- ✅ [AGENT] LX-overlays History, scan exclusions, feedback parity through AppModel/Core
- ✅ [AUTO] Merge Dependabot [#24](https://github.com/edwardlthompson/QuickMediaIngest/pull/24) (nuget-dependencies group)
- ✅ [AUTO] Merge Dependabot [#21](https://github.com/edwardlthompson/QuickMediaIngest/pull/21) (github-actions group)
- ✅ [AUTO] Close superseded Dependabot [#14](https://github.com/edwardlthompson/QuickMediaIngest/pull/14) (Magick.NET 14.14.0 → 14.15.0; main already 14.16.0, #24 → 14.17.1)

---

## Ingest-bench UX M7–RTL (2026-09-14)

- ✅ [AGENT] UX-M7 Replace non-destructive MessageBoxes with overlay/status via shared `IUserPrompt` (keep delete-after and other destructive confirms; both heads)
- ✅ [AGENT] UX-M4b Preferences search-in-settings
- ✅ [AGENT] UX-D8 Live “Imported n files → folder” with Open folder (Hedge afterglow)
- ✅ [AGENT] UX-B1 Destination summary chip in the main column (Capture One “place”, not a buried path)
- ✅ [AGENT] UX-B2 Import as a full-bleed progress scene (Hedge): list dims, big file/group progress, Open destination on done
- ✅ [AGENT] UX-B3 CommandBar overflow at narrow widths (View docks / “…”) — MinWidth 900 already shipped
- ✅ [AGENT] UX-HC High-contrast token wiring (`SystemParameters.HighContrast`)
- ✅ [AGENT] UX-AAA Import hit target 44×44 (WCAG 2.5.5)
- ✅ [AGENT] UX-RTL `FlowDirection` when an RTL locale ships (ja/de/es/fr only for now)

## Template catch-up v1.0.0 → v1.5.0 (named 1–14) (2026-09-14)

- ✅ [AGENT] T1 Fetch `edwardlthompson/agent-project-bootstrap` tag `v1.5.0`; snapshot child-only scripts; sacred denylist
- ✅ [AGENT] T2 Canon 1: copy `.cursor/commands/` including `resume.md` with `docs/BATCH_COMMANDS.md` + `docs/help/BATCH_COMMANDS.md`
- ✅ [AGENT] T3 Canon 2–3: copy `.cursor/rules/` including `product-brief.mdc` (keep `wpf-mvvm.mdc`); copy `docs/CURSOR_MODES.md` + `docs/help/`
- ✅ [AGENT] T4 Canon 4–5: additive template scripts + example stubs; merge (do not replace) `feature-gate.sh`, `validate-bootstrap.sh`, `check-license-compliance.sh`, `watch-agent-gates.sh`; keep WPF 800/400/200
- ✅ [AGENT] T5 Mixed 7–12: workflows additive only (no Pages/release-please; open-PR sync as workflow-example); union `.gitignore`; merge `bootstrap.config.json`, `TEMPLATE_INDEX.json` keys, `.env.example`, `PROJECT_CHECKLIST.md`
- ✅ [AGENT] T6 Mixed 13: stamp `AGENT.md` + `BUILD_PLAN` `product-brief-sync` from `branding/product.json` (not the template About stub)
- ✅ [AGENT] T7 Canon 6: `bash scripts/bootstrap-lifecycle.sh --sync-adapters`; empty diff on Sacred + `QuickMediaIngest/`
- ✅ [AUTO] T8 `validate-bootstrap --quick` then stamp `.template-version` / `TEMPLATE_INDEX` / `.template-update.json` to `1.5.0`
- ✅ [AUTO] T9 `feature-gate --stack dotnet-wpf` Linux subset (hygiene + license; skip WPF STA); Windows `dotnet` job in `ci.yml` unchanged
- ✅ [AUTO] T10 Trivy stays the required Security Scan bar; gitleaks/semgrep stay advisory (`continue-on-error`); `check-security-scan-policy.sh` + local config gates

## Unblock Linux host gates (named UNB) (2026-09-14)

- ✅ [AGENT] UNB-SDK Add `scripts/install-dotnet-sdk-linux.sh` (official `dotnet-install.sh`, channel 8.0, `$HOME/.dotnet`, no sudo) and run it on this machine
- ✅ [AGENT] UNB-PATH Teach `feature-gate.sh` to try `DOTNET_ROOT`, `$HOME/.dotnet/dotnet`, and WSL `dotnet.exe` before `block_env`
- ✅ [AGENT] UNB-GATE On non-Windows, do not environment-block the whole `dotnet-wpf` stack: run hygiene + `dotnet test` of `net8.0` Core.Tests when that project exists (after LX-L1); skip WPF STA tests; full sln stays Windows CI
- ✅ [AUTO] UNB-SDK-SYS Canonical Linux SDK is user-local `$HOME/.dotnet` (10a); no apt/sudo
- ✅ [AUTO] UNB-T9 Re-run `feature-gate --stack dotnet-wpf` after UNB-SDK/PATH (Linux subset after UNB-GATE); flip T9 ❌ → ✅ on pass
- ✅ [AGENT] UNB-PRS Managed Open PRs sync block on `BUILD_PLAN.md` (`open-prs-sync` begin/end)
- ✅ [AGENT] UNB-DOCS Add `docs/LINUX_DEV.md` + README: SDK install, why WPF tests stay on Windows, how Linux `/build` uses Core.Tests
- ✅ [AUTO] UNB-CI ubuntu-latest job `dotnet test` Core.Tests after LX-L1 (does not replace windows-latest WPF job)

## Golden Path 15–19 (named) (2026-09-14)

- ✅ [AGENT] GP-15 Register `dotnet-wpf` detect globs in `schemas/golden-path/feature-catalog.json` for shipped Core/overlay paths
- ✅ [AGENT] GP-19 Teach `scripts/lib/gate_scope.py` and `local_resources.py` about `QuickMediaIngest/` / `dotnet-wpf`
- ✅ [AGENT] GP-18 Rewrite `docs/features/*.md` container maps from `examples/web|android` to `QuickMediaIngest/`
- ✅ [AGENT] GP-16 Settings-chrome: Settings is the sidebar chrome entry; Theme/About/donate live in Preferences (`docs/features/settings-chrome.md`)
- ✅ [AGENT] GP-17 Overlay nav stack in Core (push/pop Settings → About → Feedback); thin ViewModel wiring

## Ingest-bench UX Q1–M6 (2026-09-14)

- ✅ [AGENT] UX-Q1 Cut dead chrome (PillToggle, leftover Theme/About strings); collapsed bell opens the notifications flyout
- ✅ [AGENT] UX-Q2 First-run onboarding (`IsFirstRun`) + empty-state title/body + Refresh beside Add FTP
- ✅ [AGENT] UX-Q3 Progressive toolbar: hide Retry/Resume/Queue/Rebuild until relevant; keep Import, Dry run, Refresh
- ✅ [AGENT] UX-Q4 Copy pack (EN + fr/es; ja/de keys) — no “small utility” / “blacklist” / “Unexpected Error”
- ✅ [AGENT] UX-Q5 AA targets: chip 24px, primary 40×32, slider names, reduced-motion blur/nudge skip
- ✅ [AGENT] UX-M Command bar + View overflow, notifications flyout, About split, 14px tokens
- ✅ [AGENT] THEME_QA via `scripts/check-theme-qa.sh` (14px body, `#007ACC`, Blue primary, 24px chip, 40px Import, first-run, command bar)
- ✅ [AGENT] UX-M6 Motion + skeletons: overlay 180ms enter, chevron 120ms, Import press scale, row fade-in, scan/thumbnail skeletons, success flash, chip collapse, sidebar 260→64; skip all if reduced motion

## LX-deb-sign v1 unsigned (2026-09-14)

- ✅ [AUTO] LX-deb-sign v1 unsigned GitHub `.deb`; `scripts/sign-deb.sh` no-ops without `DEB_GPG_KEY`

## Live Smoke & Sacred Specs Automation (2026-08-30)

- ✅ [HUMAN] Live OP13 smoke: PreferAdb browse/previews/transfer (USB debugging)
- ✅ [HUMAN] Author docs/spec.md and docs/plan.md for Quick Media Ingest. Sacred — do not paste the template stub. Agent must not create or refresh these files.

---

## Ongoing Maintenance and Feature Backlog (2026-08-30)

- ✅ [AGENT] /feature I-02: LibRaw/libvips-first decode path for common RAW; tests; no examples/ copy
- ✅ [AGENT] /feature I-03: GPS/PII strip option on embed-keywords / copy; tests; no examples/ copy
- ✅ [AGENT] /feature I-04: settings JSON export/import matching PRIVACY.md; schema version; tests; no examples/ copy
- ✅ [AGENT] /feature I-05: config schema migration on version bump; tests; no examples/ copy
- ✅ [AGENT] /feature I-06: Discard Feedback also clears clipboard we wrote; tests; no examples/ copy
- ✅ [AGENT] /feature I-07: sanitize import-report paths with privacy-report sanitizer; tests; no examples/ copy
- ✅ [AGENT] /feature I-08: optional destination encryption hint (BitLocker/VeraCrypt detect only); tests; no examples/ copy
- ✅ [AGENT] /feature I-09: Scorecard workflow green or documented exception in DECISION_LOG
- ✅ [AGENT] /feature I-10: Dependabot Magick/NuGet weekly apply ADR (what we will not bump blindly)
- ✅ [AGENT] /feature I-11: safely eject / dismount source after verified import; tests; no examples/ copy
- ✅ [AGENT] /feature I-12: dry-run import (plan only, no copy); tests; no examples/ copy
- ✅ [AGENT] /feature I-13: pick / reject cull on the review grid before Import; tests; no examples/ copy
- ✅ [AGENT] /feature I-14: star ratings + color labels persisted in import report; tests; no examples/ copy
- ✅ [AGENT] /feature I-15: per-source naming template; tests; no examples/ copy
- ✅ [AGENT] /feature I-16: copyright / creator IPTC-IIM or XMP stamp on import; tests; no examples/ copy
- ✅ [AGENT] /feature I-17: XMP sidecar write (do not mutate RAW); tests; no examples/ copy
- ✅ [AGENT] /feature I-18: destination free-space forecast from selected bytes; tests; no examples/ copy
- ✅ [AGENT] /feature I-19: collision report for Skip / Suffix / Overwrite before copy; tests; no examples/ copy
- ✅ [AGENT] /feature I-20: resume-pending-plan visible banner + one-click resume; tests; no examples/ copy
- ✅ [AGENT] /feature I-21: import history search / filter / export CSV; tests; no examples/ copy
- ✅ [AGENT] /feature I-22: SHA-256 checksum manifest next to destination shoot; tests; no examples/ copy
- ✅ [AGENT] /feature I-23: 3-2-1 second destination (copy to two roots); tests; no examples/ copy
- ✅ [AGENT] /feature I-24: watch-folder auto-scan (opt-in, local only); tests; no examples/ copy
- ✅ [AGENT] /feature I-25: eject reminder if delete-after-import left files; tests; no examples/ copy
- ✅ [AGENT] /feature I-26: milliseconds in naming live preview for all source types; tests; no examples/ copy
- ✅ [AGENT] /feature I-27: shoot-title batch rename with uniqueness check; tests; no examples/ copy
- ✅ [AGENT] /feature I-28: skip already-imported via local hash catalog across destinations; tests; no examples/ copy
- ✅ [AGENT] /feature I-29: MTP / WPD phone import without ADB; tests; no examples/ copy
- ✅ [AGENT] /feature I-30: iPhone USB (Windows portable-device) scan; tests; no examples/ copy
- ✅ [AGENT] /feature I-31: camera Wi-Fi profile presets (Sony / Canon / Nikon folder maps); tests; no examples/ copy
- ✅ [AGENT] /feature I-32: PTP / USB tether browse (FOSS libusb, no vendor SDK); tests; no examples/ copy
- ✅ [AGENT] /feature I-33: PreferAdb dual-FTP alias de-dupe; tests; no examples/ copy
- ✅ [AGENT] /feature I-34: FTP bandwidth / connection cap slider; tests; no examples/ copy
- ✅ [AGENT] /feature I-35: offline mock FTP + mock removable volume for UI tests; tests; no examples/ copy
- ✅ [AGENT] /feature I-36: DeviceWatcher live tests behind an opt-in flag
- ✅ [AGENT] /feature I-37: show transport (ADB vs FTP vs local) on each shoot row; tests; no examples/ copy
- ✅ [AGENT] /feature I-38: removable-drive throttle QA checklist + automated timing harness
- ✅ [AGENT] /feature I-39: DateTaken timezone override (camera vs local vs UTC); tests; no examples/ copy
- ✅ [AGENT] /feature I-40: manual shoot-split / merge after hours-slider; tests; no examples/ copy
- ✅ [AGENT] /feature I-41: destination folder template tokens (job / client / camera); tests; no examples/ copy
- ✅ [AGENT] /feature I-42: same card new-day rescan keeps prior cull/selection; tests; no examples/ copy
- ✅ [AGENT] /feature I-43: HEIC/HEIF decode without Magick when libvips/WIC can; tests; no examples/ copy
- ✅ [AGENT] /feature I-44: video proxy or first-frame-only mode for huge MP4s; tests; no examples/ copy
- ✅ [AGENT] /feature I-45: optional ffmpeg transcode-on-import (FOSS, off by default); tests; no examples/ copy
- ✅ [AGENT] /feature I-46: compare two files side-by-side beyond RAW+JPEG stack; tests; no examples/ copy
- ✅ [AGENT] /feature I-47: preview cache cap + purge in Preferences; tests; no examples/ copy
- ✅ [AGENT] /feature I-48: color-managed preview (system ICC, no extra CMS SDK); tests; no examples/ copy
- ✅ [AGENT] /feature I-49: GitHub duplicate search with 60s cooldown (real API, fail-soft); tests; no examples/ copy
- ✅ [AGENT] /feature I-50: follow Windows system theme (light / dark / system); tests; no examples/ copy
- ✅ [AGENT] /feature I-51: high-contrast / reduced-motion honor in overlays; tests; no examples/ copy
- ✅ [AGENT] /feature I-52: keyboard map for Feedback (Esc, Ctrl+C, Ctrl+Enter opens GitHub); tests; no examples/ copy
- ✅ [AGENT] /feature I-53: remove dead ReportBug_Click URL opener
- ✅ [AGENT] /feature I-54: pending-crash review must not re-offer a Discarded fingerprint; tests; no examples/ copy
- ✅ [AGENT] /feature I-55: add GitHub issue-form templates bug.yml and feature.yml
- ✅ [AGENT] /feature I-56: THEME_QA pass on Feedback and crash-review overlays
- ✅ [AGENT] /feature I-57: screen-reader pass: overlays announce title and live preview; tests; no examples/ copy
- ✅ [AGENT] /feature I-58: German and optional Japanese .resx; no examples/ copy
- ✅ [AGENT] /feature I-59: language change without full restart; tests; no examples/ copy
- ✅ [AGENT] /feature I-60: high-DPI 150/200 layout pass on About diagnostics WrapPanel
- ✅ [AGENT] /feature I-61: F1 shortcuts page lists Feedback / crash / eject commands
- ✅ [AGENT] /feature I-62: extract QuickMediaIngest.Core csproj; tests; no examples/ copy
- ✅ [AGENT] /feature I-64: headless --smoke-libvips plus Core unit job on Linux CI
- ✅ [AGENT] /feature I-66: README process target 0.15.1 to 1.0.0
- ✅ [AGENT] /feature I-67: write docs/FIRST_30_DAYS.md child week-1 playbook (not the template stub; not spec.md)
- ✅ [AGENT] /feature I-69: BUILD_PLAN test count + CHANGELOG Unreleased for GP 1-7
- ✅ [AGENT] /feature I-70: workflow_dispatch always uploads versioned EXE/MSI names
- ✅ [AGENT] /feature I-71: portable EXE cold-start prefs smoke in CI
- ✅ [AGENT] /feature I-72: structured JSON log option (local file, no network); tests; no examples/ copy
- ✅ [AGENT] /feature I-73: WiX repair/upgrade does not clobber AppData config; tests; no examples/ copy
- ✅ [AGENT] /feature I-74: About shows channel (portable vs MSI) and update asset that would apply; tests; no examples/ copy
- ✅ [AGENT] /feature I-75: add HUMAN checklist for authoring spec.md/plan.md; do not create those files
- ✅ [AGENT] /feature I-76: Runbook PreferAdb + OP13 + delete-after-import failure modes
- ✅ [AGENT] /feature I-77: THIRD_PARTY_LICENSES refresh after Magick/NetVips bumps
- ✅ [AGENT] /feature I-78: optional in-app What's new from local CHANGELOG only; tests; no examples/ copy
- ✅ [AGENT] /feature I-79: keyboard-only first-run onboarding (no mouse); tests; no examples/ copy
- ✅ [AGENT] /feature I-80: per-shoot keyword autocomplete from last 50 imports (local only); tests; no examples/ copy

---

## Golden Path catch-up and Sequential hybrid ADB (2026-08-30)

- ✅ [AGENT] FTP vault migrate on DHCP host change + reconnect/test success logs + listing KeepAlive=false
- ✅ [AGENT] Skip Android `.trashed-*` / trash dirs; restore FTP thumbnail limit defaults (48)
- ✅ [AGENT] Hybrid FTP browse / ADB pull import with media-root preflight + PreferAdb setting
- ✅ [AGENT] Seamless hybrid ADB scan+thumbs; HEIF normalize; 550 fail-fast; safe Magick/glitch reject; Unified scan dedupe
- ✅ [AGENT] PreferAdb polish: sibling FileExists gate; quiet 550 cache logs; ADB find+stat sizes; reconnect auto-select Unified
- ✅ [AGENT] Thumb limit on Unified/FTP; ADB vs FTP transport Info logs; cap thumb RETR parallelism=3; find+stat `|` fix
- ✅ [AGENT] Fix ADB dd quoting (sh -c) + media magic gate; no Magick on capped HEIC; Dispatcher group rebuild
- ✅ [AGENT] HEIC 12MB ADB pull + CompleteFile ADB; video full download; Unified sibling thumbs
- ✅ [AGENT] /feature GP-1: gap existing About overlay vs spec; port into current About folders + QuickMediaIngest.Tests; no examples/ copy
- ✅ [AUTO] feature-gate.sh --stack dotnet-wpf
- ✅ [HUMAN] Smoke About donate / Check now after align
- ✅ [AGENT] /feature GP-2: WPF crash-capture vertical slice from spec (opt-in, sanitize-before-persist, at-most-one); tests; no examples/ copy
- ✅ [AUTO] feature-gate.sh --stack dotnet-wpf
- ✅ [HUMAN] Smoke: setting off = no persist; setting on = one sanitized record
- ✅ [AGENT] /feature GP-3: gap existing Preferences/Settings vs spec; port into current Settings folders; no examples/ copy
- ✅ [AUTO] feature-gate.sh --stack dotnet-wpf
- ✅ [HUMAN] Smoke theme persist + crash-save toggle (after GP-2)
- ✅ [AGENT] /feature GP-4: WPF feedback dialogs from spec; wire from existing About; tests; no examples/ copy
- ✅ [AUTO] feature-gate.sh --stack dotnet-wpf
- ✅ [HUMAN] Smoke Report a bug / Request a feature from About
- ✅ [AGENT] /feature GP-5: WPF/Core github-feedback composer from spec; tests; no examples/ copy
- ✅ [AUTO] feature-gate.sh --stack dotnet-wpf
- ✅ [HUMAN] Smoke Open GitHub uses https only; offline Copy still works
- ✅ [AGENT] /feature GP-6: Core privacy-report sanitizer from spec + unit tests; no examples/ copy
- ✅ [AUTO] feature-gate.sh --stack dotnet-wpf
- ✅ [HUMAN] Confirm crash/feedback text never keeps tokens/home paths
- ✅ [AGENT] /feature GP-7: WPF display-refresh port from spec (reference Android stub only); tests or documented fallback; no examples/ copy
- ✅ [AUTO] feature-gate.sh --stack dotnet-wpf
- ✅ [HUMAN] Smoke About/Settings scroll on a high-refresh display
- ✅ [AGENT] /feature I-01: bump Magick.NET past current GHSA set; tests; no examples/ copy
- ✅ [AGENT] /feature I-63: split remaining 400-line ViewModel partials (UiState at cap)
- ✅ [AGENT] /feature I-65: make Golden Path / Sequential HUMAN rows parseable by build-sprint-status
- ✅ [AGENT] /feature I-68: Feedback overlay ViewModel tests (preview enablement, https-only)
- ✅ [HUMAN] WPF UI sign-off via scripts/run-human-signoffs.ps1 when shipping product changes

---

## Donate and filename-version updates (2026-08-21)

- ✅ [AGENT] Donate via Venmo + filename-version GitHub update prompt (device-local prefs, once-per-version nudge)
- ✅ [AGENT] Ship v1.3.27; document in CHANGELOG / DECISION_LOG

---

## Release v1.3.22 — Settings persistence (2026-07-28)

- ✅ [AGENT] Fix destination + naming prefs wiped on restart (load guards, Custom coerce, destination combo restore)
- ✅ [AGENT] Split `MainViewModel.Naming.partial.cs`; destination/naming config round-trip tests
- ✅ [AGENT] Fix `check-github-ci.sh` per-workflow poll (Dependabot Updates hid CI)
- ✅ [AGENT] Ship v1.3.22; document in KNOWLEDGE_BASE / DECISION_LOG

---

## Bootstrap alignment 0.11 → 0.15.1 (2026-07-22)

- ✅ [AGENT] Write `docs/BOOTSTRAP_ALIGNMENT.md` + DECISION_LOG ADR
- ✅ [AGENT] Sync agent docs/rules/commands + Cursor FOSS pack; seed `HUMAN_BACKLOG.md`
- ✅ [AGENT] Hooks dry-run (`check-cursor-hooks` + `validate-local` + feature-gate)
- ✅ [AGENT] Merge validate-bootstrap; cherry-pick allowlisted scripts
- ✅ [AGENT] Update `modules/dotnet-wpf` links + TEMPLATE_INDEX additive paths
- ✅ [AGENT] Restructure BUILD_PLAN + README agent section + AGENT_MEMORY provenance
- ✅ [AGENT] S5 validate; bump `.template-version` to 0.15.1
- ✅ [AGENT] Automate Align HUMAN_BACKLOG (stale/weekly/automerge; decline release-please/pages)
- ✅ [HUMAN] Dependabot alerts API verified; WPF sign-off for Align cycle

---

## Release v1.3.21 — Import progress UI freeze (2026-07-21)

- [x] [AGENT] Non-blocking import progress (`BeginInvoke` + coalesce byte snapshots)
- [x] [AGENT] Ship v1.3.21; document mid-card hang in `KNOWLEDGE_BASE.md`
- [x] [HUMAN] Confirmed recovered import folder; card cleanup after verify

---

## Release v1.3.20 — SD/USB stall + SBOM (2026-07-17)

- [x] [AGENT] `RemovableDriveIo` + preview cancel on import + StaRunner Shell fallback + cancel propagation
- [x] [AGENT] Fix `count-critical-high-dependabot.sh` (`--paginate`, no `page=`)
- [x] [AGENT] Ship v1.3.20 (`chore(release)` + Build workflow_dispatch); 151 Release tests
- [x] [AUTO] CI / Security Scan / CodeQL green on `ae84019`; SBOM asset on GitHub Release
- [x] [HUMAN] Optional: validate SD card preview+import with portable v1.3.20

---

## Sprint 0 — Bootstrap Parity (2026-06-13)

### Sequential

- [x] [HUMAN] Click **Use this template** *(N/A — existing repo, Reference mode)*
- [x] [AGENT] Fill placeholders in `docs/INITIALIZATION_PROMPT.md`
- [x] [AGENT] Add agent scaffolding: `AGENTS.md`, `docs/START_HERE.md`, memory files, `.cursor/rules/`
- [x] [HUMAN] Enable Dependabot alerts + security updates
- [x] [HUMAN] Enable private vulnerability reporting + branch protection on `main`
- [x] [AGENT] Create `SECURITY.md`, `CODE_OF_CONDUCT.md`, `docs/THREAT_MODEL.md`, `docs/PRIVACY.md`, `docs/RUNBOOK.md`
- [x] [AGENT] Add `.github/CODEOWNERS` and `THIRD_PARTY_LICENSES.md`
- [x] [AGENT] Initialize workspace memory files
- [x] [AGENT] Wire update checker config (weekly)
- [x] [HUMAN] Set GitHub repo About description from `docs/GITHUB_ABOUT.md`
- [x] [AGENT] Add `.env.example`
- [x] [AGENT] Ensure `TEMPLATE_INDEX.json` complete
- [x] [AGENT] Add `modules/dotnet-wpf/MODULE.md`
- [x] [AGENT] Wire `QuickMediaIngest.Tests` into solution; add `.editorconfig`
- [x] [AGENT] Add CI workflows (`ci.yml`, `codeql.yml`, `security.yml`, `dependency-review.yml`, `dependabot.yml`)
- [x] [AUTO] `scripts/check-file-encoding.sh` passes *(verified locally; CI after push)*
- [x] [AUTO] `scripts/validate-bootstrap.sh` passes *(verified locally; CI after push)*
- [x] [AUTO] `dotnet test` passes *(20 tests, Release; CI green on `main` as of v1.3.2)*
- [x] [HUMAN] Approve Sprint 0 *(CI confirmed green after push)*

### Parallel

- [x] [AGENT] Update README bootstrap sections
- [x] [HUMAN] Configure `.template-update.json` interval (weekly)
- [x] [AUTO] Pre-commit config present (`.pre-commit-config.yaml`)

---

## Sprint 1 — File Size Remediation (2026-06-13)

**Goal:** Split oversized WPF files to meet adapted line limits; remove grandfather entries from `scripts/check-file-limits.sh`.

**Adapted limits:** `.xaml` 800, ViewModels/`*.xaml.cs` 400, `Core/` 200 lines.

### Sequential

- [x] [AGENT] Split `MainWindow.xaml` into overlay UserControls (`DialogOverlays`, `ImportHistoryOverlay`, `PreferencesOverlay`, `ScanExclusionsOverlay`); sidebar/import remain in shell (755 lines, under 800)
- [x] [AGENT] Split `MainWindow.xaml.cs` into partials (`Chrome`, `Ribbon`, `Settings`); moved converters to `Converters/`
- [x] [AGENT] Split `MainViewModel.cs` into 18 partial files + `SupportTypes.cs`; retained `Config` and `Tokens` partials
- [x] [AGENT] Split `App.xaml.cs` → `App.Theme.partial.cs` (under 400-line code-behind limit)
- [x] [AUTO] `scripts/check-file-limits.sh` passes with empty grandfather list *(verified locally)*
- [x] [AUTO] `dotnet test` passes *(13 tests, Release)*
- [x] [HUMAN] Approve Sprint 1 *(CI confirmed green after push)*

### Parallel

- [x] [AGENT] Extract Preferences overlay UserControl — `QuickMediaIngest/Controls/PreferencesOverlay/`
- [x] [AGENT] `MainViewModel.Scan.partial.cs`, `MainViewModel.Import.partial.cs` (and related partials)
- [-] [AGENT] Extract Sidebar UserControl — deferred (not required; `MainWindow.xaml` under limit)
- [-] [AGENT] Extract Import panel UserControl — deferred (not required; `MainWindow.xaml` under limit)

### Tooling added

- `tools/split_mainviewmodel.py`, `tools/split_mainwindow.py`, `tools/split_mainwindow_cs.py`

---

## Sprint 2 — Release Readiness & v1.3.2 (2026-06-13)

**Release:** [v1.3.2](https://github.com/edwardlthompson/QuickMediaIngest/releases/tag/v1.3.2)

### Agent work

- [x] [AGENT] Fix Settings **Save & Close** — `SaveAndCloseSettingsCommand`; wired `PreferencesOverlayView`
- [x] [AGENT] Unify folder naming — `Core/GroupFolderNaming.cs` shared by `GroupBuilder` and `IngestEngine`
- [x] [AGENT] Fix CodeQL workflow — `init` before `dotnet build`
- [x] [AGENT] Gate `build.yml` on `dotnet test`; release/tag/upload only on `workflow_dispatch`
- [x] [AGENT] `ci.yml`: vulnerable packages, license compliance, `dotnet format --verify-no-changes`
- [x] [AGENT] `FolderNamingTests.cs`, `KeywordInputParserTests.cs`, settings save-and-close test + `RUNBOOK.md` QA
- [x] [AGENT] Bump Magick.NET `14.13.0` → `14.14.0` *(NU190 advisories cleared)*
- [x] [AGENT] SQLite index on `DeviceId` + `Path` *(already in `DatabaseService`)*

### Human / release closure

- [x] [HUMAN] Push to `main` *(commit `c805aac`)*
- [x] [AUTO] `ci.yml`, `codeql.yml`, `security.yml` green on `main`
- [x] [HUMAN] Sprint 0/1 CI sign-off
- [x] [HUMAN] Release v1.3.2 — `CHANGELOG.md`, GitHub Release with portable EXE, zip, MSI

### Post-push CI fixes

- [x] [AGENT] Grandfather Sprint 3 Core files in `check-file-limits.sh` pending splits
- [x] [AGENT] Pin .NET SDK `8.0.422` via `global.json`
- [x] [AGENT] Pin `trivy-action` to v0.35.0 SHA (0 open Dependabot alerts)

**Commits:** `c805aac`, `8c5666a`, `f4d5a5a`, `9c01f23`

---

## Sprint 3 — Persistence & Dead DI (2026-06-13)

- [x] [HUMAN→AGENT] Persistence strategy **B** — JSON config + VACUUM-only SQLite (`DECISION_LOG.md`)
- [x] [AGENT] Slim `IDatabaseService` / `DatabaseService` to `TryPeriodicVacuum()` only
- [x] [AGENT] Remove `IMetadataReader`, `IWhitelistFilter` from DI; delete `WhitelistFilterTests`

---

## Sprint 4 — Quick Wins (2026-06-13)

- [x] [AGENT] Delete dead code-behind: `Token_*`, `Settings_MoveToken*`, `Settings_Save`/`Settings_Close`
- [x] [AGENT] `SelectAllCheckBox` → `SelectAllShootsCommand` / `DeselectAllShootsCommand`
- [x] [AGENT] Translate `A11y_NotificationsIcon` in `Strings.es.resx`, `Strings.fr.resx`

---

## Sprint 5 — Tests (partial, 2026-06-13)

- [x] [AGENT] `FolderNamingTests.cs` — import/export folder naming parity
- [x] [AGENT] `KeywordInputParserTests.cs` (4 cases)

---

## Sprint 6 — Infrastructure (2026-06-13)

- [x] [AGENT] `scripts/validate-local.ps1`
- [x] [AGENT] `RestorePackagesWithLockFile` + `packages.lock.json`
- [x] [AGENT] Expand `docs/FOR_AGENTS.md`; add `docs/DEV_SETUP_WINDOWS.md`
- [x] [AGENT] Pre-commit: `check-file-limits.sh`, `check-license-compliance.sh`
- [x] [AGENT] Bump test packages: xunit 2.9.2, Test.Sdk 17.11.1
- [x] [AGENT] `Microsoft.Extensions.*` already at 8.0.1 (latest 8.0.x patch)

---

## Milestone Gates — Closed (through v1.3.2)

| Gate | Sprint | Closed |
|------|--------|--------|
| Regression tests: zero failures (20 tests) | 2 | 2026-06-13 |
| Settings Save & Close persists config | 2 | 2026-06-13 |
| Import folder names = export folder names | 2 | 2026-06-13 |
| No dead DI registrations | 3 | 2026-06-13 |
| CodeQL init before build | 2 | 2026-06-13 |
| Release gated on tests | 2 | 2026-06-13 |
| `dotnet format` enforced in CI | 2 | 2026-06-13 |
| NuGet audit clean in CI | 2 | 2026-06-13 |
| Static analysis and vulnerability scans clean | 2 | 2026-06-13 |
| `CHANGELOG.md` updated | 2 | 2026-06-13 |
| Version bumped + GitHub Release (v1.3.2) | 2 | 2026-06-13 |
| Windows local validation script | 6 | 2026-06-13 |
| ViewModel/`*.xaml.cs` line limits | 1 | 2026-06-13 |
| UTF-8 encoding / template index / bootstrap artifacts | 0 | 2026-06-13 |
| Zero open Critical/High Dependabot alerts | — | 2026-06-13 |
---

## Post-Push Checklist (closed 2026-06-13)

- [x] [AUTO] Local bootstrap artifact check
- [x] [AUTO] Local file line limits check
- [x] [AUTO] Local `dotnet test` (20 passed)
- [x] [AUTO] `ci.yml` all jobs green on `main`
- [x] [AUTO] `codeql.yml` and `security.yml` green on `main`
- [x] [HUMAN] Sprint 0/1 CI approval after green run on `main`

---

## Pre-Bootstrap Milestones

---

## Milestone 1 – Bug Fixes & Foundation

### Sprint 1.1: Core Bug Fixes & Licensing

- [x] Fix `ItemProcessed` event signature in `IngestEngine.cs`
- [x] Add MIT license file (`LICENSE`) and credit third-party assets

### Sprint 1.2: Documentation & Nullability

- [x] Add basic XML documentation comments to public APIs in Core & Providers
- [x] Enable nullable reference types project-wide (`#nullable enable`)

### Sprint 1.3: Logging & Test Scaffolding

- [x] Add initial unit tests (xUnit + Moq) for:
  - `IngestEngine.ResolveFileName`
  - `GroupBuilder` logic
  - Whitelist filter matching

---

## Milestone 2 – Architecture & Developer Experience

### Sprint 2.1: Dependency Injection

- [x] Introduce Microsoft.Extensions.DependencyInjection
- [x] Register all services/providers in `App.xaml.cs`
- [x] Inject into `MainViewModel`, `IngestEngine`, providers, etc.

### Sprint 2.2: MVVM Toolkit Migration

- [x] Migrate `MainViewModel` to CommunityToolkit.Mvvm
- [x] Use `[ObservableProperty]`, `[RelayCommand]`
- [x] Remove manual `INotifyPropertyChanged` + `ICommand` boilerplate

### Sprint 2.3: Model & Logging Improvements

- [x] Convert suitable models to `record` types
- [x] Add structured logging levels throughout ingestion flow

---

## Milestone 3 – Performance & Speed Optimizations

### Sprint 3.1: Parallelization & Caching

- [x] Parallelize file copy loop in `IngestEngine.IngestGroupAsync`
- [x] Implement thumbnail disk cache in `%AppData%\QuickMediaIngest\Thumbnails\`

### Sprint 3.2: FTP & Indexing Optimizations

- [x] Optimize FTP provider (binary mode, FluentFTP, batch listings)

---

## Milestone 4 – UX/UI Polish & Usability Wins

### Sprint 4.1: Settings & Preview

- [x] Naming template live preview
- [x] Whitelist rule manager (add/edit/delete)
- [x] Toggle move/copy, post-import delete option

### Sprint 4.2: Progress & Grid Enhancements

- [x] Dedicated Ingest Log panel
- [x] Overall + per-group progress bars
- [x] Working cancel button (propagate CancellationToken)
- [x] Hover zoom / right-click full preview
- [x] Better multi-select (Shift/Ctrl)
- [x] Ensure virtualization works with large sets
- [x] Add first-run onboarding tooltips / tour

---

## Milestone 5 – High-Value Features

### Sprint 5.1: Post-Import & Filtering

- [x] Quick dark/light mode toggle in title bar
- [x] Post-import actions (open folder, eject, sidecar export)
- [x] Advanced grid filtering / search (date range, file type, keyword)

### Sprint 5.2: Duplicates & Stretch Goals

- [x] Duplicate detection across sources
- [x] ADB fallback for faster Android transfers
- [x] Export/import app settings (JSON)

---

## Nice-to-Have / Future Ideas

- [x] Better error recovery (retry failed files, skip vs abort)

---

## Sprint 3 — Core Integrity (2026-06-13)

**Goal:** Enforce Core 200-line limit; remove grandfather entries from `check-file-limits.sh`.

### Sequential

- [x] [AGENT] Split `Core/FtpScanner.cs` → `FtpListingParser`, `FtpDirectoryClient`, `FtpScanPlanner`, `FtpScanProgress` (195 lines)
- [x] [AGENT] Split `Core/IngestEngine.cs` → `IngestFileNaming`, `IngestVerification`, `IngestItemProcessor` (162 lines)
- [x] [AGENT] Split `Core/ThumbnailService.cs` → `ThumbnailDiskCache`, `ExifThumbnailReader`, `ShellThumbnailInterop` (198 lines)
- [x] [AGENT] Split `Core/ServiceContracts.cs` → `IDatabaseService` in `Data/`; factories in `Core/Factories/` (101 lines)
- [x] [AUTO] `scripts/check-file-limits.sh` passes with empty grandfather list for Core

### Parallel (partial)

- [x] [AGENT] Extract shared media-extension constants — `Core/MediaExtensions.cs` (`IsRawExtension`, extended video list)
- [-] [AGENT] Unify FTP stacks — deferred to backlog
- [-] [AGENT] Decouple `ShootFilterService` from ViewModels — deferred to backlog
- [x] [AGENT] Remove WPF `BitmapSource` from Core contracts — done as R2-D1 / F-002 (2026-07-12)

---

## Sprint 4 — ViewModel & UI Cleanup (partial, 2026-06-13)

### Sequential

- [x] [AGENT] Rename `MainViewModel.Part9–Part17` → semantic partials (`Thumbnails`, `FtpThumbnails`, `UnifiedSource`, `ImportEngine`, `ImportPostProcess`, `ImportExecution`, `DriveExclusions`, `DriveScan`, `Onboarding`)
- [x] [AGENT] Rename `Filters` → `FtpWorkflow`, `Updates` → `SourceLoad`; moved `BuildUpdateHandoffScript` to `Import.partial.cs`
- [x] [AGENT] Add `ViewModels/GlobalUsings.cs` (non-conflicting project globals)
- [x] [AGENT] Overlay decouple (partial): `PreferencesOverlay`, `ScanExclusionsOverlay`, `ImportHistoryOverlay` bind to VM commands; code-behind emptied
- [x] [AGENT] `DialogOverlaysView` + remaining MainWindow handlers — complete (see P2 / § DialogOverlaysView below)

---

## Sprint 5 — Test Coverage Expansion (2026-06-13)

**Baseline:** 51 unit tests, all passing (`dotnet test`).

| Component | Tests added |
|-----------|-------------|
| `FtpListingParser` | 10+ cases (`FtpListingParserTests`) |
| `IngestFileNaming` duplicate policies | 4 cases (`IngestFileNamingTests`) |
| `IngestVerification` strict/fast | 2 cases (`IngestVerificationTests`) |
| `DatabaseService` injectable path | `DatabaseServiceTests` |
| `LocalScanner` extension filter | 3 cases (`LocalScannerTests`) |
| `MediaExtensions` | `MediaExtensionsTests` |
### Ongoing maintenance (agent)

- [x] [AGENT] Merged Dependabot PR #3 (github-actions group)
- [x] [AGENT] Dependabot PR #4 — safe bumps applied locally; PR closed (MD5/Extensions 10 deferred)
- [x] [AGENT] CVE triage automation pass — no open critical alerts (2026-06-13)

---

## Sprint 4 — ViewModel & UI Cleanup (complete, 2026-06-13)

- [x] [AGENT] `IFileDialogService` / `IShellService` + WPF implementations; wired into `BrowseDestination`, About actions
- [x] [AGENT] `DialogOverlaysView` decoupled — `PasswordBoxAssist`, `OpenLogsFolderCommand`, `ReportBugCommand`; code-behind emptied
- [x] [AGENT] FTP workflow status strings moved to `Strings.resx` (`Vm_Ftp_*` keys)
- [x] [AGENT] `AutomationProperties.Name` on collapsed sidebar icon buttons
- [x] [AGENT] `ShootFilterService` decoupled from Localization — uses `Core/Models/FilterFileTypeIds`

---

## ADB Provider Testing (2026-06-13)

**Device:** `b5214fc6` (physical Android, USB debugging)

| Test | Result |
|------|--------|
| `AdbDeviceProbe.ListDeviceSerials` | Pass — 1 device |
| `AdbFileProvider.CopyAsync` pull from `/sdcard/DCIM/*.jpg` | Pass — file copied to temp |
**New code:** `Core/AdbDeviceProbe.cs`, `Tests/AdbFileProviderTests.cs`

**Test baseline:** 53 unit tests, all passing.

---

## Milestone 7 — FTP Preview Reliability (v1.3.6, 2026-06-13)

### Sprint 7.1: Diagnose + Reliability

- [x] [AGENT] Analyze FTP thumbnail failure (code evidence: empty `ftp.Pass`, no download retries, full-file download, broken retry path)
- [x] [AGENT] Add `FtpSourceCredentials.ResolvePassword` + `EnsureFtpSourceCredentials`; apply at scan, thumbnail, unified, reconnect
- [x] [AGENT] Extract `FtpFileDownloader` (retries, canonical `FtpUriBuilder`, zero-byte guard, `ILogger`)
- [x] [AGENT] Fix `RetryFailedPreviewLoadsAsync` for `IsFtpSource` items
- [x] [AGENT] Unit tests: `FtpUriBuilderTests`, `FtpSourceCredentialsTests`, `FtpPreviewDownloadLimitsTests`

### Sprint 7.2: Performance + Consolidation

- [x] [AGENT] `FtpThumbnailBatchFetcher` with FluentFTP capped stream download (512 KB images, 4 MB video, 2 MB RAW)
- [x] [AGENT] `IFtpThumbnailService` / `FtpThumbnailService` consolidates FTP + unified thumbnail paths
- [x] [AGENT] Remove `DebugAgentLog` instrumentation

### Sprint 7.3: Release

- [x] [AGENT] Bump to v1.3.6; 80 unit tests passing
- [ ] [HUMAN] Manual smoke: `10.0.0.23:2221/DCIM` — thumbnails load fast and reliably
- [ ] [AUTO] CI green after push

**New code:** `Core/Ftp/FtpUriBuilder.cs`, `FtpFileDownloader.cs`, `FtpThumbnailBatchFetcher.cs`, `FtpPreviewDownloadLimits.cs`, `Core/Services/FtpThumbnailService.cs`, `FtpSourceCredentials.cs`, `FtpEndpoint.cs`

**Test baseline:** 80 unit tests, all passing.

---

## Milestone 8 — Thumbnail Performance + Settings Persistence (v1.3.12, 2026-06-13)

### Sprint 8.1: Settings persistence

- [x] [AGENT] `DeleteAfterImportConfirmHelper` — safety dialog only on user-initiated toggle; prompt dismissed only after OK
- [x] [AGENT] Delete After Import checkbox in Preferences Import Settings
- [x] [AGENT] `ConfigPersistenceTests` round-trip for `DeleteAfterImport` + `DeleteAfterImportPromptDismissed`

### Sprint 8.2: Thumbnail performance

- [x] [AGENT] CPU-scaled `GetFtpThumbnailWorkerCount()` (Ultra up to 16 workers)
- [x] [AGENT] Thumbnail Performance hint in Preferences (en/fr/es)
- [x] [AGENT] FTP thumbnail disk cache (`host|port|remotePath|fileSize`) in `ThumbnailDiskCache` + `FtpThumbnailService`
- [x] [AGENT] Skip redundant DNG FTP download when same-stem HEIC/JPG in batch (`GroupRawAndRenderedPairs`)

### Sprint 8.3: Release

- [x] [AGENT] Bump to v1.3.12; `CHANGELOG.md` updated
- [x] [AGENT] Deploy test build to Desktop (`QuickMediaIngest.exe` v1.3.12.0)
- [x] [AGENT] 85 unit tests passing (excl. integration/smoke)
- [ ] [HUMAN] Delete After Import + Ultra speed + FTP cache smoke on LAN FTP source

---

## Milestone 9 — FTP Thumbnail Pipeline v2 (v1.3.13, 2026-06-13)

### Sprint 9.1: Tier 1 — Fetch strategy

- [x] [AGENT] Zoom persistence: Ctrl+wheel + slider aligned to 50–300; `OnThumbnailSizeChanged` clamp; `ConfigPersistenceTests` for `ThumbnailSize`
- [x] [AGENT] `FtpPreviewDownloadLimits` tier API (64K → 256K → 512K → cap); HEIC cap lowered to 2 MB
- [x] [AGENT] `FtpTieredPreviewLoader` + `FtpThumbnailPipeline` — tiered download with decode-after-each-tier
- [x] [AGENT] ViewModel FTP disk cache pre-check (`FtpThumbnailCache.TryLoad`) before scheduling network work
- [x] [AGENT] `FtpPreviewDownloadTierTests`, `HeicEmbeddedPreviewReaderTests`

### Sprint 9.2: Tier 2 — Pipeline architecture

- [x] [AGENT] Viewport-priority thumbnail queue (expanded / top shoot groups first)
- [x] [AGENT] Split download pool (capped at 6) vs decode pool (`FtpThumbnailLoadOptions`)
- [x] [AGENT] `FtpStreamingDownloader` per-host FluentFTP connection pool (Max/Ultra); `FtpWebRequest` fallback

### Sprint 9.3: Tier 3 — Advanced decode + transport

- [x] [AGENT] `HeicEmbeddedPreviewReader` — partial-buffer JPEG segment extraction before Magick
- [x] [AGENT] NetVips / `VipsThumbnailDecoder` shrink-on-load path; Magick fallback

### Sprint 9.4: Release

- [x] [AGENT] Bump to v1.3.13; `CHANGELOG.md` updated
- [x] [AGENT] Deploy test build to Desktop (`QuickMediaIngest.exe` v1.3.13.0)
- [x] [AGENT] 89 unit tests passing (excl. integration/smoke)
- [ ] [HUMAN] Cold load `10.0.0.23:2221/DCIM` — tiered bytes in log; cache hit on reconnect; zoom persists; FluentFTP + libvips on Max/Ultra

---

## Template Migration Sprint — v0.11.0 (2026-06-20)

Aligned with upstream [agent-project-bootstrap](https://github.com/edwardlthompson/agent-project-bootstrap) **v0.11.0** (was pinned v0.2.0). Critique mitigations in `DECISION_LOG.md`.

- [x] [AGENT] Phases 1–3 — slash commands, Cursor rules, gate scripts, CI jobs (`batch-commands`, `repo-hygiene`, WPF `feature-gate.sh`)
- [x] [AGENT] Phase 4 — `TEMPLATE_INDEX.json`, docs (`AGENTS.md`, `FOR_AGENTS.md`, `START_HERE.md`, etc.), `.template-version` bump
- [x] [AGENT] Phase 5 — gate suite + 101 tests; super-command smoke in `DECISION_LOG.md`
- [x] [HUMAN] Slash-command `/` menu sign-off; template version bump approval
- [x] [AUTO] CI green — `e59ec6f`, CRLF fix `7720986`

---

## Human Verification Automation (2026-06-20)

Automated BUILD_PLAN HUMAN rows for v1.3.12 / v1.3.13 acceptance:

- [x] [AGENT] `LanFtpSmokeProbe` + `HumanVerificationSmokeTests` — tier caps, reconnect cache, Ultra mode (skip when LAN FTP offline)
- [x] [AGENT] `ConfigFilePersistenceTests`, `MainViewModelConfigReloadTests` — Delete After Import, `ThumbnailSize`
- [x] [AGENT] `DeleteAfterImportConfirmHelperTests`, `PublishedExeSmokeTests`
- [x] [AGENT] `scripts/smoke-human-verification.ps1/.sh`, `scripts/smoke-published-exe.ps1/.sh`
- [x] [AGENT] `--smoke-libvips` headless flag; `NetVips.Native.win-x64` for single-file publish
- [x] [AUTO] Optional CI job for human-verification smoke; `--smoke-libvips` in `ci.yml`

**Test baseline:** 106 unit tests (Release).

---

## Human Sign-off Automation (2026-06-21)

Single orchestrator replaces BUILD_PLAN HUMAN checklist items:

- [x] [AGENT] `scripts/run-human-signoffs.ps1/.sh` — tests, security triage, optional push/CI poll
- [x] [AGENT] `scripts/ensure-gh-security-scope.sh` — Dependabot API probe + optional `gh auth refresh`
- [x] [AGENT] `HumanSignoffVerificationTests` — Delete After Import + ThumbnailSize bindings after config reload
- [x] [AGENT] STA `WpfTestFixture` for reliable headless WPF control tests
- [x] [AUTO] CI job `human-signoffs` with `GITHUB_TOKEN` security-events read

**Test baseline:** 109 unit tests (Release).

---

## Import Progress + ETA — F-002 (2026-06-21)

Byte-weighted import progress with parallel copies (up to 8) unchanged:

- [x] [AGENT] `ImportByteProgressTracker` + `ImportByteProgressSnapshot` — thread-safe completed + in-flight bytes
- [x] [AGENT] `IFileProvider.CopyAsync` optional `IProgress<long>` — Local, FTP, ADB providers
- [x] [AGENT] `IngestItemProcessor` start/complete events; `MainViewModel.ImportProgress.partial.cs` shared ETA/bar
- [x] [AGENT] `ImportByteProgressTrackerTests` + `IngestEngineTests` incremental copy progress

**Test baseline:** 113 unit tests (Release).

---

## Backlog Parallel Lane — P1–P8 (2026-06-20)

| P | Task | Status |
|---|------|--------|
| 1 | Published portable exe libvips + native DLL smoke | ✅ |
| 2 | `DialogOverlaysView` + remaining MainWindow handlers | ✅ |
| 3 | Grandfather file-limit cleanup (MainViewModel + Core/Ftp) | ✅ |
| 4 | Headless WPF smoke in CI | ✅ |
| 5 | Core architecture (`object?` thumbnail, `ShootFilterService`, FTP stack unification) | ✅ |
| 6 | UI UserControl extractions | ✅ N/A — `MainWindow.xaml` under 800-line limit |
| 7 | MSI admin-extract validation in `build.yml` | ✅ |
| 8 | Template update check + OpenSSF Scorecard workflow | ✅ |
### P3 file splits (grandfather cleared)

- `MainViewModel.ConfigLoad.partial.cs`, `FtpCollections.partial.cs`, `ThumbnailsLoad.partial.cs`
- `FtpDownloadSync.cs`, `FtpTieredPreviewDecoder.cs`, `FtpThumbnailPipeline.{Fetch,Tiered,Helpers}.cs`
- `ThumbnailService.Decode.partial.cs`

### P7 / P8 CI

- `scripts/validate-msi-install.ps1` — administrative MSI extract + libvips smoke
- `.github/workflows/scorecard.yml`, `scripts/check-scorecard-sarif.sh`

---

## Audit Sprint R1 — 2026-06-21 (partial)

Full report: `CODE_REVIEW.md` (gitignored, local only).

### Completed

- [x] [AGENT] F-002 — `scripts/check-readme-health.sh` (README sections + doc link validation)
- [x] [AGENT] F-003 — Gitignore `CODE_REVIEW.md` + `build_errors.md`; untrack stale `build_errors.md`
- [x] [AUTO] Gate verification — bootstrap, feature-gate (9 stages), hygiene, readme health, 106 tests
- [x] [AGENT] F-001 — P1–P8 backlog + v1.3.17 release (shipped `8556015`)

### Optional (not blocking)

- [ ] [HUMAN] F-004 — `gh auth refresh -s security_events` (local Dependabot strict only; CI uses `GITHUB_TOKEN`)
- [ ] [HUMAN] Optional live UI glance — headless sign-off tests cover bindings

---

## Audit Sprint R2 — 2026-07-12

Full report: `CODE_REVIEW.md` (gitignored, local only).

### Completed

- [x] [AGENT] F-001/F-010 — Purge legacy `FtpPass` from `config.json` after Credential Manager migrate; WPF + persistence tests
- [x] [AGENT] F-003 — Document Win32 `LocalMachine` = per-user persistent (no `LocalUser` in Meziantou enum)
- [x] [AGENT] F-004 — Crash dump reads `config.json` and redacts `FtpPass`
- [x] [AGENT] F-005 — Collapse `..` / `.` in `FtpPathNormalizer` + `FtpListingParser`; unit tests
- [x] [AGENT] F-013–F-016 — Docs: AGENT_MEMORY versions, MODULE checklist, README callouts, COMPLETED_TASKS DialogOverlays row
- [x] [AUTO] Gates — bootstrap, feature-gate (9 stages), watch-agent-gates; **127** Release tests
- [x] [AGENT] F-002 / R2-D1 — Remove WPF `BitmapSource` from Core thumbnail contracts; `DecodedThumbnail` + `WpfThumbnailBridge`; WPF orchestration under `Thumbnails/Wpf/`; **144** Release tests green
- [x] [AGENT] F-006 / R2-D2 — `LogPathSanitizer` + Tier-1 Information/Error logs (scan, ingest, FTP, AppData)
- [x] [AGENT] F-008 / R2-D3 — Unit tests: MetadataKeywordWriter, IngestItemProcessor, UpdateService, LogPathSanitizer

### Human follow-ups (completed 2026-07-12)

- [x] [AGENT] F-011 — Merged Dependabot PRs [#10](https://github.com/edwardlthompson/QuickMediaIngest/pull/10) (NuGet), [#7](https://github.com/edwardlthompson/QuickMediaIngest/pull/7) (Actions)
- [x] [AGENT] F-012 — Scorecard root cause: workflow-level `security-events`/`id-token` write rejected by `publish_results`; fixed in `14bdfcb`; verified [run 29203183074](https://github.com/edwardlthompson/QuickMediaIngest/actions/runs/29203183074)

---

## Audit R2 Backlog D1–D3 — 2026-07-12

- [x] R2-D1 — Core thumbnail pipeline WPF-free (`DecodedThumbnail`)
- [x] R2-D2 — Log path sanitization
- [x] R2-D3 — Core coverage tests (UpdateService / IngestItemProcessor / MetadataKeywordWriter)

**Deferred (still open, low priority):** `FtpScanner` live-FTP tests, `DeviceWatcher` WMI tests, separate `QuickMediaIngest.Core` class library.

---

## AUTO-SBOM — 2026-07-12

- [x] [AGENT] CycloneDX SBOM via `anchore/sbom-action@v0.24.0` (Syft) on `./publish/portable` in `.github/workflows/build.yml`
- [x] [AGENT] Upload `QuickMediaIngest-<version>.cyclonedx.json` as CI artifact and GitHub Release asset on `workflow_dispatch`
- [x] [AGENT] Local helper `scripts/generate-sbom.sh` + `TEMPLATE_INDEX.json` entry
