# Post-push CI poll

After pushing to main, poll required GitHub workflows until green:

```bash
bash scripts/check-github-ci.sh --wait 300
```

Required: **CI**, **Security Scan**, **CodeQL**. Do not mark release or Sprint 0 complete while any fail.

**`[HUMAN]` offline fallback:** If `gh` is unavailable or `--wait` times out, open GitHub Actions manually and confirm the three workflows above are green on `HEAD` before signing off. Document the manual check in `DECISION_LOG.md`.

Begin now.
