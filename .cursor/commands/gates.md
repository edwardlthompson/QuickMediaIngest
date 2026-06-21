# Local validation gates

Run Sprint 0 / pre-push validation. **Windows (recommended):**

```powershell
.\scripts\validate-local.ps1
```

**Git Bash / WSL:**

```bash
bash scripts/validate-bootstrap.sh --quick
bash scripts/feature-gate.sh --stack dotnet-wpf
bash scripts/check-repo-hygiene.sh
```

Report pass/fail per script. Fix failures in scope before marking BUILD_PLAN items complete.

Begin now.
