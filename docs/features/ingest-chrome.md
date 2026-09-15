# Feature: ingest-chrome

> Calm ingest bench: one Import hero, waiting-for-card empty state, progressive toolbar, readable type.

## Acceptance criteria

- ✅ First run shows onboarding once (`IsFirstRun`; skip with `QMI_SKIP_ONBOARDING=1`)
- ✅ Empty list: headline + body + Refresh (primary) and Add FTP (secondary)
- ✅ First-run toolbar shows Import, Dry run, Refresh, Delete after import, and View overflow (Retry/Resume/Queue/Rebuild only when relevant)
- ✅ Notifications live in a working bell flyout, not a sidebar log dump
- ✅ About is version + updates + donate; glossary is F1 only
- ✅ AA: chip remove 24×24; primary actions ≥44×32; sliders named; reduced motion skips overlay blur and ribbon nudge
- ✅ Tokens: 12/14/16/20 type, accent `#007ACC`, no Excel-yellow Material primary
- ✅ i18n: EN copy pack plus fr/es; ja/de fallback keys for empty/dry-run/a11y

## Backlog (BUILD_PLAN 23–32)

- ✅ UX-M6 Motion + scan/thumbnail skeletons; skip all if reduced motion
- ✅ UX-M7 Non-destructive MessageBoxes → overlay/status via shared `IUserPrompt` (both heads)
- ✅ UX-M4b Preferences search-in-settings
- ✅ UX-D8 Live “Imported n files → folder” with Open folder
- ✅ UX-B1 Destination summary chip
- ✅ UX-B2 Import progress in the status/notification bar (no list dim, no full-screen meter)
- ✅ UX-B3 Narrow-width CommandBar overflow
- ✅ UX-HC High-contrast tokens
- ✅ UX-AAA 44px Import
- ✅ UX-RTL when an RTL locale ships
- ✅ THEME_QA — `scripts/check-theme-qa.sh` (Windows visual glance optional)

Two-head Linux .deb (BUILD_PLAN **33–42**): archived. Linux function parity (BUILD_PLAN **43–71**): `docs/features/linux-parity.md`.

## Smoke scenario

1. Given a first-run config
2. When the app starts, the user dismisses onboarding, then sees “No sources yet”
3. Then Refresh and Add FTP are visible; Import is the filled primary; Retry/Queue/Resume are hidden

## Container map

| Layer | Path |
|-------|------|
| View | `QuickMediaIngest/MainWindow.xaml`, overlays, `OnboardingDialog.xaml`, `QuickMediaIngest.Desktop/MainWindow.axaml` |
| Logic | `MainViewModel.Nav.partial.cs`, `MainViewModel.Prompt.partial.cs`, `MainWindow.xaml.cs` first-run, `IngestBenchAppModel` compact/notifications |
| Tokens | `App.xaml`, `Themes/Typography.xaml`, `Themes/Colors.xaml` |
| Tests | `QuickMediaIngest.Tests/UxChromeTests.cs`, `QuickMediaIngest.Tests/UserPromptTests.cs`, `QuickMediaIngest.Core.Tests/CommandBarOverflowTests.cs`, `scripts/lib/test_motion_timings.py`, `scripts/lib/test_user_prompt.py`, `scripts/lib/test_command_bar_overflow.py` |
| Wiring | `TryShowFirstRunOnboarding` ≤10 lines after crash review; Desktop `SizeChanged` → `CommandBarOverflow.IsCompact` |

## Tests

- Automated: yes — `UxChromeTests` / `UserPromptTests` (Windows) + `scripts/check-theme-qa.sh` + `scripts/lib/test_user_prompt.py` (any host)
- Command: `bash scripts/check-theme-qa.sh`

## Fallback validation

- Why WPF STA tests are not feasible here: Linux hosts without .NET 8 cannot run `net8.0-windows` tests
- Command: Windows CI `dotnet` job in `.github/workflows/ci.yml`
