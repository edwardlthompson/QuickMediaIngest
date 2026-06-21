# Pre-release gate

```bash
bash scripts/pre-release-gate.sh
```

Confirm CI + Security Scan + CodeQL green, zero Critical/High Dependabot alerts, `.template-version` present.
Do not tag or `/push` until this gate passes.

**`[HUMAN]` offline fallback:** If `check-github-ci.sh --wait` times out, verify workflows manually on GitHub before release.

Begin now.
