# Decision Log

> Append-only register of major technical trade-offs. Past entries are immutable.

## 2026-06-13 — Persistence strategy B (JSON + VACUUM-only SQLite)

**Decision:** Keep JSON files for config, import history, and presets; slim `IDatabaseService` to `TryPeriodicVacuum()` only; remove unused SQLite CRUD APIs and dead DI (`IMetadataReader`, `IWhitelistFilter`).

**Rationale:** App already persists all user-facing state via JSON under `%AppData%\QuickMediaIngest\`. SQLite tables were unused split-brain. VACUUM retains occasional DB file maintenance without migration cost.

**Alternatives rejected:** (A) migrate config/history to SQLite — higher risk, no user benefit today.

**Validation:** `dotnet build`, `dotnet test`; `DatabaseService` no longer exposes CRUD.

---

## 2026-06-13 — Sprint 1 closed; file limits enforced

**Decision:** Close Sprint 1 file size remediation; archive to `COMPLETED_TASKS.md`; defer Sidebar/Import UserControl extraction (shell XAML under 800-line limit).

**Validation:** `scripts/check-file-limits.sh` (empty grandfather list), `dotnet test` (13 passed).

---

## 2026-06-13 — Sprint 0 closed; GitHub settings applied

**Decision:** Close Sprint 0 bootstrap parity; archive tasks to `COMPLETED_TASKS.md`; active work moves to Sprint 1 (file size remediation).

**GitHub settings applied via `gh`:** Dependabot security updates enabled, private vulnerability reporting enabled, branch protection on `main` (requires CI status checks), repo About description and topics updated.

**Pending:** Push bootstrap + Sprint 1 to `main`; confirm new `ci.yml` / `codeql.yml` / `security.yml` workflows green.

---

## 2026-06-13 — Magick.NET 14.14.0

**Decision:** Bump `Magick.NET-Q16-AnyCPU` from 14.13.0 to 14.14.0.

**Rationale:** Clears NU1902/NU1903 vulnerability advisories on restore/build without API changes; 13 tests pass.

**Validation:** `dotnet build`, `dotnet test` (Release).

---

**Decision:** Adopt [agent-project-bootstrap](https://github.com/edwardlthompson/agent-project-bootstrap) scaffolding without copying web/python/android Golden Path examples.

**Rationale:** QuickMediaIngest is a mature .NET 8 WPF app; Reference mode preserves existing architecture while adding agent routing, CI guardrails, and workspace memory.

**Alternatives rejected:** Full greenfield re-scaffold; copying unrelated stack examples.

**Validation:** `scripts/validate-bootstrap.sh`, `ci.yml` dotnet test job.

---

## 2026-06-13 — ADR-0001: MVVM + provider architecture (existing)

**Decision:** MVVM with CommunityToolkit.Mvvm partials; `IFileProvider` abstraction for Local/FTP/ADB sources; Core logic in `QuickMediaIngest/Core/` without WPF dependencies.

**Rationale:** Testability, extensibility for new source types, separation of UI from ingest pipeline.

**See also:** `docs/adr/0001-core-architecture.md`
