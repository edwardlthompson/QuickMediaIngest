# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.3.x   | :white_check_mark: |
| < 1.3   | :x:                |

## Reporting a Vulnerability

**Do not** open public GitHub issues for security vulnerabilities.

1. Use GitHub **Private vulnerability reporting** (Security → Advisories → Report a vulnerability), or
2. Contact the maintainers listed in `.github/CODEOWNERS` with:
   - Description of the vulnerability
   - Steps to reproduce
   - Impact assessment
   - Suggested fix (if any)

## Response Timeline

| Stage | Target |
|-------|--------|
| Acknowledgment | 3 business days |
| Initial assessment | 7 business days |
| Fix or mitigation plan | 30 days (severity-dependent) |
| Public disclosure | Coordinated with reporter |

## Security Practices

- Dependabot alerts and weekly CVE triage: see [`docs/SECURITY_TRIAGE.md`](docs/SECURITY_TRIAGE.md)
- Secrets must never be committed (Gitleaks pre-commit enforced)
- FTP credentials stored in Windows Credential Manager, not in config files
- Report dependency vulnerabilities via Dependabot; do not commit patched forks without review
