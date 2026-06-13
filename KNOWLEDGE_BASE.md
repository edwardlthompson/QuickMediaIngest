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
