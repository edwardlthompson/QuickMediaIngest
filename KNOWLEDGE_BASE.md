# Knowledge Base

> Stack-specific edge cases, resolved bugs, anti-patterns, and reusable solutions.
> Do not populate with generic framework definitions.

## SQLite + Single-File Publish

`System.Data.SQLite` resolves native interop using `Assembly.Location`. Default single-file publish keeps managed DLLs in-memory so `Location` is empty and `SQLiteConnection` throws `ArgumentNullException` in `Path.Combine`.

**Fix:** `<IncludeAllContentForSelfExtract>true</IncludeAllContentForSelfExtract>` in `QuickMediaIngest.csproj`.

## Theme QA

Before large UI changes, run through `docs/THEME_QA_CHECKLIST.md` and `tools/check_theme_contrast.py`.

## FTP Credentials

Stored via `WindowsFtpCredentialStore` (Windows Credential Manager). Never log passwords or store in plain-text config.

**v1.3.19:** `LoadConfig` must rewrite `config.json` with empty `FtpPass` after vault migrate — otherwise plaintext can remain until the user saves settings.

## OpenSSF Scorecard `publish_results`

Workflow-level `security-events: write` / `id-token: write` causes Scorecard API 400. Use `permissions: read-all` at workflow scope and write perms only on the analysis job.

## Dependabot alert count script pagination

`scripts/count-critical-high-dependabot.sh` can fail with HTTP 400 when using `page=` on Dependabot alerts API. Prefer unpaginated `gh api .../dependabot/alerts?state=open` or refresh `gh` scopes; treat zero Critical/High from a successful API query as release-ready when `--strict` count script errors.

## SD card / USB preview + import stall

High `Parallel.ForEach` preview workers + Shell decode via `Dispatcher.Invoke` + concurrent import copies thrash removable media and can freeze the UI.

**Fix:** `RemovableDriveIo` caps preview workers (≤2) and import copies (1) on removable drives; local preview `ParallelOptions` honor cancel; import start cancels preview CTS; Shell/WPF fallback uses `StaRunner` (not UI dispatcher); `IngestItemProcessor` rethrows `OperationCanceledException`.
