# Prompt Library

> Effective prompt strategies for this repository.

## Reference Mode (existing project)

```
Read @docs/START_HERE.md, @docs/CURSOR_MODES.md, and @docs/FOR_AGENTS.md.
Follow Reference Read Order.
Use BUILD_PLAN.md Sequential lane first; respect AGENT/HUMAN/ADB/AUTO labels.
Active module: @modules/dotnet-wpf/MODULE.md
Slash commands: @docs/BATCH_COMMANDS.md
```

## Bootstrap Mode (new from template)

```
Read @docs/START_HERE.md and @docs/INITIALIZATION_PROMPT.md.
Follow Section 8 Startup Sequence.
Use BUILD_PLAN.md Sequential lane first; respect AGENT/HUMAN/ADB/AUTO labels.
```

## WPF UI change

```
Read @docs/THEME_QA_CHECKLIST.md and @.cursor/rules/wpf-mvvm.mdc before editing XAML.
Inspect existing MaterialDesignThemes patterns in QuickMediaIngest/Themes/.
Include ### Critique for modal overlay order and localization (.resx).
```

## Release prep

```
Run dotnet test -c Release.
Confirm weekly CVE triage per @docs/SECURITY_TRIAGE.md.
Update CHANGELOG.md (Keep a Changelog format).
Bump <Version> in QuickMediaIngest.csproj.
```

## Local compute / parallel scope

```
Prefer This Computer + parallel Task/worktrees per @.cursor/rules/local-compute.mdc.
Use /scope and @docs/PARALLEL_AGENT_SCOPES.md; do not parallel-edit BUILD_PLAN.md or TEMPLATE_INDEX.json.
```

## Cleanup

```
Run /cleanup per @.cursor/commands/cleanup.md after a sprint or failed gate loop.
Archive completed rows to COMPLETED_TASKS.md; clear stale session state.
```

## Bootstrap alignment

```
Read @docs/BOOTSTRAP_ALIGNMENT.md and @HUMAN_BACKLOG.md before adopting new upstream CI workflows.
Preserve modules/dotnet-wpf and .github/workflows/build.yml.
```
