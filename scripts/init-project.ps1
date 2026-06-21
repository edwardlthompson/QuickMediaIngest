# Post-template clone customization helper
# Usage: .\scripts\init-project.ps1 [-Reference] [-NoPrune] [-Stack dotnet-wpf] ...
param(
    [string]$Stack = "",
    [string]$ProjectName = "",
    [string]$ProjectPurpose = "",
    [string]$Interval = "",
    [switch]$Reference,
    [switch]$NoPrune,
    [switch]$NonInteractive
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

function Invoke-PyScript {
    param([string[]]$ScriptArgs)
    if (Get-Command python -ErrorAction SilentlyContinue) { & python @ScriptArgs; return }
    if (Get-Command python3 -ErrorAction SilentlyContinue) { & python3 @ScriptArgs; return }
    throw 'Python not found'
}

Write-Host "=== agent-project-bootstrap init ===" -ForegroundColor Cyan
if ($Reference) { Write-Host "(Reference mode — preserving customized docs)" -ForegroundColor Yellow }
Write-Host ""

if (-not $NonInteractive) {
    if (-not $ProjectName) { $ProjectName = Read-Host "Project name" }
    if (-not $ProjectPurpose) { $ProjectPurpose = Read-Host "One-line purpose" }
    if (-not $Stack) { $Stack = Read-Host "Primary stack (dotnet-wpf/web/python/android/node/multi/none) [dotnet-wpf]" }
    if (-not $Interval) { $Interval = Read-Host "Template update check interval (off/daily/weekly/monthly/on_session) [weekly]" }
}
if (-not $Stack) { $Stack = "dotnet-wpf" }
if (-not $Interval) { $Interval = "weekly" }

if (-not $Reference) {
    foreach ($file in @("docs/INITIALIZATION_PROMPT.md", "AGENT_MEMORY.md")) {
        $path = Join-Path $Root $file
        if (Test-Path $path) {
            $content = Get-Content $path -Raw
            $content = $content -replace '\[INSERT PLATFORM / TECH STACK HERE\]', $Stack
            $content = $content -replace '\[INSERT DETAILED APP DESCRIPTION AND GOALS HERE\]', $ProjectPurpose
            [System.IO.File]::WriteAllText($path, $content)
        }
    }

    $configPath = Join-Path $Root ".template-update.json"
    $config = Get-Content $configPath -Raw | ConvertFrom-Json
    $config.check_interval = $Interval
    $config | ConvertTo-Json -Depth 5 | Set-Content $configPath -Encoding UTF8

    $CodeOwner = Read-Host "GitHub username for CODEOWNERS (without @)"
    if ($CodeOwner) {
        $codeownersPath = Join-Path $Root ".github/CODEOWNERS"
        if (Test-Path $codeownersPath) {
            $co = Get-Content $codeownersPath -Raw
            $co = $co -replace '@\[PROJECT_OWNER\]', "@$CodeOwner"
            [System.IO.File]::WriteAllText($codeownersPath, $co)
        }
    }

    $About = "$ProjectName — $ProjectPurpose. Built with agent-project-bootstrap. FOSS MIT."
    @"
# GitHub About Block

## Draft Description (edit to ≤350 chars)

$About

## Topics

Add topics relevant to your project and stack.
"@ | Set-Content (Join-Path $Root "docs/GITHUB_ABOUT.md") -Encoding UTF8
}

Invoke-PyScript -ScriptArgs @("scripts/sync-stack-config.py", $Root, "", "")

$pruned = "false"
if (-not $Reference -and -not $NoPrune) {
    if (-not $NonInteractive) {
        $Prune = Read-Host "Prune unused examples/modules? (y/N)"
    } else {
        $Prune = "N"
    }
    if ($Prune -eq "y" -or $Prune -eq "Y") {
        $pruned = "true"
        switch ($Stack) {
            "web" { Remove-Item -Recurse -Force examples/python, examples/android, examples/node, modules/python, modules/android, modules/node -ErrorAction SilentlyContinue }
            "python" { Remove-Item -Recurse -Force examples/web, examples/android, examples/node, modules/web, modules/android, modules/node -ErrorAction SilentlyContinue }
            "android" { Remove-Item -Recurse -Force examples/web, examples/python, examples/node, modules/web, modules/python, modules/node -ErrorAction SilentlyContinue }
            "dotnet-wpf" {
                Remove-Item -Recurse -Force examples -ErrorAction SilentlyContinue
                Remove-Item -Recurse -Force modules/web, modules/python, modules/android, modules/node, modules/lightroom, modules/rust, modules/go -ErrorAction SilentlyContinue
            }
            default { Write-Host "Keeping all examples (multi-stack)" }
        }
    }
} else {
    Write-Host "Skipping prune (reference mode or -NoPrune)."
}

Invoke-PyScript -ScriptArgs @("scripts/init-stack-sync.py", $Stack, $Root, $pruned)
Write-Host "Wrote .cursor/stack-selection.json and synced AGENT_MEMORY active modules."

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Green
Write-Host "Stack selection: .cursor/stack-selection.json"
if ($Reference) {
    Write-Host "Reference mode complete — customized docs preserved."
}
