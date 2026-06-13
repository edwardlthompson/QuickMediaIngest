# Threat Model

> STRIDE threat model for Quick Media Ingest desktop application.

## Scope

| Item | Value |
|------|-------|
| Project | Quick Media Ingest |
| Stack | .NET 8 WPF, Windows desktop |
| Methodology | STRIDE |

## Trust Boundaries

```text
[User] --> [WPF UI / ViewModels] --> [Core / IngestEngine] --> [File Providers] --> [Local / FTP / ADB]
                |                           |
         AppConfig / SQLite          Windows Credential Manager
                |
         Destination filesystem (user-chosen)
```

## STRIDE Summary

| Threat | Example | Mitigation | Owner |
|--------|---------|------------|-------|
| Spoofing | Malicious FTP server | TLS where supported; user confirms sources | AGENT |
| Tampering | Modified config SQLite | Local-only storage; user backups | AGENT |
| Repudiation | Denied import action | Import reports in `_ImportReports` | AGENT |
| Information disclosure | FTP creds in logs | Credential Manager; no password logging | AGENT |
| Denial of service | Huge FTP directory listing | Scan limits, cancellation tokens | AGENT |
| Elevation of privilege | Path traversal on import | Path normalization, whitelist filters | AGENT |

## Top Abuse Cases

1. **Malicious FTP server** — crafted paths or credential phishing via fake Wi-Fi camera share
2. **Path traversal** — import destination or source paths containing `..` sequences
3. **Supply-chain compromise** — malicious NuGet dependency
4. **Secret leakage** — FTP passwords committed to config or logs
5. **Updater MITM** — tampered release binary (mitigated by HTTPS GitHub Releases)

## Security Tasks

Link mitigations to `BUILD_PLAN.md` Ongoing Maintenance and `docs/SECURITY_TRIAGE.md` weekly triage.

## Review Cadence

- `[HUMAN]` Review at each milestone boundary
- `[AGENT]` Update when architecture or data flows change (append ADR reference)
