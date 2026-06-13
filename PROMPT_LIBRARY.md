# Prompt Library

> Effective prompt strategies for this repository.

## Reference Mode (existing project)

```
Read @docs/START_HERE.md and @docs/FOR_AGENTS.md.
Follow Reference Read Order.
Use BUILD_PLAN.md Sequential lane first; respect AGENT/HUMAN/ADB/AUTO labels.
Active module: @modules/dotnet-wpf/MODULE.md
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
