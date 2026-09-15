# Theme QA Checklist

Use this checklist before merging UI changes to keep light/dark mode consistent and avoid text-contrast regressions.

**Automated gate (required):** `bash scripts/check-theme-qa.sh` — 14px body, `#007ACC` accent, Material Blue primary, 24×24 chip remove, 44px Import, first-run onboarding, Import/Dry run/Refresh command bar. A Windows visual glance is optional once that script passes.

## Test Setup

- Build and run the latest local app build.
- Start with one local source and one FTP source so both paths are visible.
- Load enough media to show:
  - grouped shoots
  - preview thumbnails
  - status/progress updates
  - notification feed updates

## Global Theme Checks

- Toggle between dark and light themes in Preferences → Appearance.
- Confirm no Excel-yellow primary on chrome (accent is `#007ACC`; yellow is warning-only).
- Confirm body text is at least 14px and captions at least 12px.
- Confirm filter-chip remove is at least 24×24 and Import is at least 44px tall.
- Confirm no controls keep stale colors after toggling.
- Confirm tooltips remain readable in both themes.

## Main Window Checks

- Top command bar:
  - Import is the filled primary; Dry run and Refresh stay visible
  - View overflow holds select-all, grouping, zoom, filter, rebuild
  - hover/focus states are visible in both themes
- Group cards:
  - shoot titles, metadata labels, and folder paths are readable
  - expand/collapse chevrons remain visible in both themes
- Bottom status bar:
  - status and scan lines are readable against the bar background

## Sidebar Checks

- Expanded mode:
  - section icons and labels are left-aligned and consistent
  - Notifications bell opens the feed flyout; status bar keeps the live line
  - Settings rows are aligned and readable
- Collapsed mode:
  - collapse/expand trigger remains obvious and clickable
  - icon rail spacing/alignment is stable (no vertical jump)
  - logo placeholder keeps controls aligned

## Overlay/Dialog Checks

- User prompt overlay (OK / Cancel) uses the same paper/backdrop tokens as other dialogs.
- Add FTP dialog text and labels remain readable in both themes.
- About/Settings dialogs keep heading/body contrast consistent.

## Functional Checks Tied To UI

- `Expand All Groups` toggles all groups on first click.
- Per-group expand/collapse works consistently after toggling `Expand All Groups`.
- `Rebuild Previews` still clears cache and reloads previews.
- Notification feed logs new status messages during scan/import.

## Regression Sweep

- Resize window from narrow to wide:
  - command bar wraps; View stays in overflow
  - sidebar remains usable in both expanded/collapsed modes
- Navigate key flows:
  - scan source
  - dry run
  - import
  - retry/rebuild previews
- Verify no clipped text, overlapping controls, or invisible icons.

## Pass/Fail Rule

Do not ship if any item below is true:

- unreadable text in any theme
- missing/low-contrast critical controls (expanders, import actions, Appearance dropdown)
- inconsistent control typography between similar controls
- broken expand/collapse behavior
