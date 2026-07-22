# Pre-release gate

```bash
python3 scripts/agent-run.py pre-release-gate
```

Confirm CI + Security Scan + CodeQL green, zero Critical/High Dependabot alerts, `.template-version` present.
Do not tag or `/push` until this gate passes. Child product (QMI): use `.\scripts\run-human-signoffs.ps1` and `bash scripts/pre-release-gate.sh`.

Begin now.
