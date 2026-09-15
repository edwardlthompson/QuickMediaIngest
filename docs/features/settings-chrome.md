# Feature: settings-chrome

> Minimal home chrome: Settings is the sidebar chrome entry. Theme, About, and donate live in Preferences.

## Acceptance criteria

- ✅ Sidebar chrome has Preferences (Settings), not a dedicated Theme or About row
- ✅ Theme, About, and donate never appear in home sidebar chrome
- ✅ Theme is a dropdown under Settings → Appearance (light / dark)
- ✅ Settings opens About via App info (does not keep Theme/donate in the header)
- ✅ Donate links live only under About
- ✅ Glossary lives in F1 shortcuts help, not About
- ✅ Offline: menus render from local state
- ✅ i18n: `Settings_Section_Appearance`, `Settings_OpenAppInfo`, theme labels

## Smoke scenario

1. Given the app is at home
2. When the user opens Preferences, changes theme via the dropdown, then opens App info
3. Then the sidebar never showed Theme/About/donate; About lists version and donate; glossary is on F1; Escape returns one overlay level

## Container map

| Layer | Path |
|-------|------|
| View | `QuickMediaIngest/Controls/PreferencesOverlay/`, `MainWindow.xaml` sidebar |
| Logic | `MainViewModel` theme + OpenAboutFromSettings |
| Tests | `QuickMediaIngest.Tests/OverlayNavTests.cs`, `GoldenPathAutomationSmokeTests.GP3_*` |
| Wiring | `ToggleSettingsCommand` |

## Tests

- Automated: yes — overlay nav + GP-3 persist
- Command: `python3 scripts/agent-run.py feature-gate --stack dotnet-wpf`
