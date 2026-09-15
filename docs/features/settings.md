# Feature: settings

> Second vertical-slice exemplar (Sprint 2) for child repos after About (Sprint 1).

## Acceptance criteria

- ✅ User can open a Settings panel/screen from the main navigation
- ✅ Theme preference (light/dark/system) persists across restarts
- ✅ Optional **Save crash details for me to review** toggle (default off); see `docs/features/feedback.md` and `docs/features/crash-capture.md`
- ✅ Daily GitHub installer checks are not gated here (see `docs/features/donations-updates.md`)
- ✅ Offline: settings load last persisted values; no network required for display
- ✅ Filter Preferences with a search box (hides unmatched sections)
- ✅ i18n: all user-visible strings under `settings.*` keys

## Smoke scenario

1. Given the app is running with default theme
2. When the user opens Settings and switches theme to dark
3. Then the UI applies dark theme immediately and still uses dark theme after cold restart

## Container map

| Layer | Path |
|-------|------|
| Logic | `QuickMediaIngest/ViewModels/MainViewModel.Config*.partial.cs`, `AppConfig` |
| View | `QuickMediaIngest/Controls/PreferencesOverlay/` |
| Tests | `QuickMediaIngest.Tests/SettingsJsonExportImportTests.cs`, `GoldenPathAutomationSmokeTests.GP3_*`, `QuickMediaIngest.Tests/SettingsSearchTests.cs` |
| Wiring | `MainViewModel` ToggleSettings (≤10 lines) |
## Tests

- Automated: yes — `QuickMediaIngest.Tests/SettingsJsonExportImportTests.cs`, `GoldenPathAutomationSmokeTests.GP3_*`

## Fallback validation

- Why tests are not feasible: N/A (automated tests exist)
- Command: `python3 scripts/agent-run.py feature-gate --stack dotnet-wpf`

## Out of scope (Sprint 2)

- Account sync, cloud backup, analytics
- Donation URL editing (stays in About / `donations.json`)

## Notes

- Reuse `AppThemeMode` / `IsDarkTheme` in Core; Appearance lives under Preferences (`docs/features/settings-chrome.md`)
- Gate after each AGENT BUILD_PLAN step: `bash scripts/watch-agent-gates.sh --once --autofix --step <scaffold|tests|wire>`
