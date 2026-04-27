# Release notes — Quick Media Ingest **v1.2.1** (2026-04-28)

## Highlights

Patch release focused on **update notifications**, **sidebar notification UX**, **config/view-model structure**, **theme resource consolidation**, and **import history** presented as a **modal overlay** consistent with the rest of the app.

---

## Updates

- When a **new GitHub release** is detected (newer than the running build), the app can show a **popup** so users do not rely only on status text in About or the sidebar.
- Alerting is **deduplicated by release tag** (`LastNotifiedUpdateTag` in `config.json`) so repeated background checks do not spam the same version.
- **About → Check interval** still controls how often the service calls GitHub; **last successful check time** remains in `%AppData%\QuickMediaIngest\last_update_check.txt`.

---

## Notifications sidebar

- **Check for updates** was removed from this panel (it remains under **About & Updates**).
- **Mute** on the notification feed was removed.
- **Delete after import** appears **first** in the readiness lines so important safety state is visible before file counts populate.

---

## UI / shell

- **F1** opens a **shortcuts** help window.
- **Import history** uses the same **modal overlay** pattern as other dialogs (the old standalone window was removed).
- **Theme** resources were consolidated (MaterialDesign **BundledTheme** + shared brushes); obsolete per-theme dictionaries were dropped.

---

## Developers

- **`CHANGELOG.md`** is the version history.
- Version is defined in **`QuickMediaIngest.csproj`** (`<Version>`).

---

### Upgrade notes

Existing `config.json` remains compatible; new keys appear when relevant preferences are saved. Optional: delete `LastNotifiedUpdateTag` only if you want to see the update popup again for the same GitHub tag after dismissing it.

Portable and MSI channels behave as in v1.2.0; use **About → Check for updates** after publish.
