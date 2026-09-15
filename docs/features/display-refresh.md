# Feature: display-refresh

Window requests the fastest same-resolution display mode. Scroll surfaces vote HIGH so adaptive-refresh panels can ramp during flings.

## Acceptance criteria

- User-visible behavior: About and Settings scroll at the panel's peak same-resolution rate when the OS allows it
- Offline/error behavior: missing display or empty mode list leaves `preferredDisplayModeId` unchanged
- Accessibility: no new controls; motion follows OS refresh (respects battery saver / ARR)
- i18n: N/A — no user-facing strings

## Smoke scenario

1. Given the app is running on a high-refresh device
2. When About or Settings is opened and flung
3. Then the window's preferred mode matches the fastest same-size `Display.Mode` and scroll uses `FrameRateCategory.High`

## Container map

| Layer | Path |
|-------|------|
| Logic | `QuickMediaIngest/Core/DisplayRefresh/DisplayModeSelector.cs` |
| Adapter | `QuickMediaIngest/Services/WindowRefresh.cs` |
| Tests | `QuickMediaIngest.Tests/DisplayModeSelectorTests.cs` |
| Wiring | `MainWindow.xaml.cs` one call |
## Tests

- Automated: yes — `QuickMediaIngest.Tests/DisplayModeSelectorTests.cs`

## Fallback validation

- Why tests are not feasible: N/A (automated tests exist)
- Command: `python3 scripts/agent-run.py feature-gate --stack dotnet-wpf`

## Notes

Fallback validation: `python3 scripts/agent-run.py feature-gate --stack dotnet-wpf`
