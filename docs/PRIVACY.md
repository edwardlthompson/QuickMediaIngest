# Privacy Policy

> Quick Media Ingest is a local-first desktop application.

## Data We Collect

| Data | Purpose | Lawful Basis | Retention |
|------|---------|--------------|-----------|
| App settings | Feature functionality | Legitimate interest | Until user deletes app data |
| Import history | User workflow | Legitimate interest | Until user clears history |
| FTP credentials | Connect to user-configured servers | User consent (explicit add) | Until user removes source |
| Log files | Diagnostics | Legitimate interest | Local disk; user can delete |

## Data We Do Not Collect

- No analytics or tracking telemetry
- No sale of personal data
- No cloud upload of media or metadata
- No PII in logs (passwords, full file paths to sensitive locations avoided where possible)

## Network Activity

| Action | Endpoint | When |
|--------|----------|------|
| Check for updates | `api.github.com` (releases) | User-initiated or optional background check |
| FTP transfer | User-configured FTP servers | During scan/import |

## User Rights (GDPR / CCPA)

- **Access:** Settings and history stored locally; user can export settings (JSON)
- **Deletion:** Uninstall app or clear `%AppData%\QuickMediaIngest\`
- **Opt-out:** No telemetry to opt out of
- **Portability:** Export/import app settings (JSON) supported

## Data Minimization

- Collect only what each feature requires
- Local-first storage; no server-side processing
- FTP credentials in Windows Credential Manager, not plain text

## DPIA Checklist (`[HUMAN]`)

Not required for typical local-only photo ingest. Revisit if cloud sync or telemetry is added.

## Contact

Privacy inquiries: see maintainers in `.github/CODEOWNERS` or `SECURITY.md`.
