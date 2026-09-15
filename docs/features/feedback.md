# Feature: feedback

> About / Help review dialogs for bug and feature reports. Not a donate nag.

## Acceptance criteria

- ✅ User-visible behavior: About has Report a bug and Request a feature; review panel shows escaped preview, Copy, Open GitHub, Discard
- ✅ Offline/error behavior: Copy still works; Open GitHub disabled with i18n reason; search fail-soft
- ✅ Accessibility: dialog `role="dialog"` with labelled buttons; no Android Toast
- ✅ i18n: `feedback.*` web / `feedback_*` Android

## Smoke scenario

1. Given crash-capture is off
2. When the user opens About and Report a bug, types a description
3. Then they can copy sanitized markdown; Open GitHub is enabled only when description or stack exists

## Container map

| Layer | Path |
|-------|------|
| View | `QuickMediaIngest/Controls/FeedbackOverlay/` |
| Logic | `QuickMediaIngest/ViewModels/MainViewModel.Feedback.partial.cs` |
| Tests | `QuickMediaIngest.Tests/MainViewModelFeedbackTests.cs`, `GoldenPathAutomationSmokeTests.GP4_*` |
| Wiring | `MainViewModel` ReportBug / RequestFeature |
## Tests

- Automated: yes — `QuickMediaIngest.Tests/MainViewModelFeedbackTests.cs`, `GoldenPathAutomationSmokeTests.GP4_*`

## Fallback validation

- Why tests are not feasible: N/A (automated tests exist)
- Command: `python3 scripts/agent-run.py feature-gate --stack dotnet-wpf`

## Definition of Done

See `docs/FEATURE_MODULES.md`. XSS test: preview never uses `innerHTML` of reporter text.

## Notes

- Settings toggle “Save crash details for me to review” defaults off (`feedback.save_crashes`)
- Discard best-effort clears clipboard text we wrote
