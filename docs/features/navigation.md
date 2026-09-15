# Feature: navigation

> WPF overlay stack for Settings / About / Feedback. Not History/`BackHandler`.

## Acceptance criteria

- ✅ Navigation is a stack, not booleans alone
- ✅ Opening App info from Settings becomes About; Report a bug pushes Feedback
- ✅ Escape/Back pops one level; pop at home is a no-op
- ✅ Serialize / deserialize persist the route stack
- ✅ About → Report bug stack is home → about → feedback
- ✅ Notifications bell opens a flyout (not a sidebar dump)

## Smoke scenario

1. Given home
2. When the user opens Settings → App info, then Report a bug
3. Then the stack is about → feedback; first Escape shows About; second is home

## Container map

| Layer | Path |
|-------|------|
| Logic | `QuickMediaIngest/Core/Nav/OverlayNav.cs` |
| View | existing overlays; flags synced from `Peek` |
| Tests | `QuickMediaIngest.Tests/OverlayNavTests.cs` |
| Wiring | `MainViewModel.Nav.partial.cs` ≤10 lines per command |

## Tests

- Automated: yes — `OverlayNavTests`
- Command: `python3 scripts/agent-run.py feature-gate --stack dotnet-wpf`
