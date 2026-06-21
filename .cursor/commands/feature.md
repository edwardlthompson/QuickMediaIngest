# Feature vertical slice step

Execute the active BUILD_PLAN feature row only (one feature per task). Scope: `QuickMediaIngest/**` and `QuickMediaIngest.Tests/**` per `modules/dotnet-wpf/MODULE.md`.

After each AGENT step:

```bash
bash scripts/watch-agent-gates.sh --once --autofix --step scaffold
```

Use `--step tests` or `--step wire` when appropriate.

**3-strike halt:** On exit **2**, `watch-agent-gates.sh` has hit environment block or 3 consecutive gate failures (`agent-progress.json` strikes ≥ 3). **Stop looping** — switch to `/debug` or escalate to `[HUMAN]`.

Begin now.
