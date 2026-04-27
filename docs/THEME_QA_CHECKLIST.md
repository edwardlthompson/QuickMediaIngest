# Theme QA Checklist

Use this checklist before merging UI changes to keep light/dark mode consistent and avoid text-contrast regressions.

## Test Setup

- Build and run the latest local app build.
- Start with one local source and one FTP source so both paths are visible.
- Load enough media to show:
  - grouped shoots
  - preview thumbnails
  - status/progress updates
  - notification feed updates

## Global Theme Checks

- Toggle between dark and light themes in Settings.
- Confirm no text becomes unreadable in either theme.
- Confirm no controls keep stale colors after toggling.
- Confirm tooltips remain readable in both themes.

## Main Window Checks

- Top toolbar:
  - section headers are readable
  - button, checkbox, and slider labels use consistent text styling
  - hover/focus states are visible in both themes
- Group cards:
  - shoot titles, metadata labels, and folder paths are readable
  - expand/collapse chevrons remain visible in both themes
- Bottom status bar:
  - status and scan lines are readable against the bar background

## Sidebar Checks

- Expanded mode:
  - section icons and labels are left-aligned and consistent
  - Notifications and Notification Feed text are readable
  - Settings rows (including Theme row) are aligned and readable
- Collapsed mode:
  - collapse/expand trigger remains obvious and clickable
  - icon rail spacing/alignment is stable (no vertical jump)
  - logo placeholder keeps controls aligned

## Overlay/Dialog Checks

- Scan/import overlays keep the underlying theme visible (translucent backdrop).
- Add FTP dialog text and labels remain readable in both themes.
- About/Settings dialogs keep heading/body contrast consistent.

## Functional Checks Tied To UI

- `Expand All Groups` toggles all groups on first click.
- Per-group expand/collapse works consistently after toggling `Expand All Groups`.
- `Rebuild Previews` still clears cache and reloads previews.
- Notification feed logs new status messages during scan/import.

## Regression Sweep

- Resize window from narrow to wide:
  - top toolbar blocks wrap cleanly
  - sidebar remains usable in both expanded/collapsed modes
- Navigate key flows:
  - scan source
  - preflight
  - import
  - retry/rebuild previews
- Verify no clipped text, overlapping controls, or invisible icons.

## Pass/Fail Rule

Do not ship if any item below is true:

- unreadable text in any theme
- missing/low-contrast critical controls (expanders, import actions, theme toggle)
- inconsistent control typography between similar controls
- broken expand/collapse behavior
