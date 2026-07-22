#Requires -Version 5.1
<#
.SYNOPSIS
  Automate Align-0.15 HUMAN_BACKLOG rows (workflows + gh scopes + AUTOMERGE_TOKEN).

.EXAMPLE
  .\scripts\automate-human-backlog.ps1
  .\scripts\automate-human-backlog.ps1 -RefreshGh -SetupAutomergeToken
#>
param(
    [switch]$RefreshGh,
    [switch]$SetupAutomergeToken,
    [switch]$SetupGithubRepo,
    [switch]$Strict
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

# Propagate Windows gh auth into Git Bash / WSL child processes.
if (-not $env:GH_TOKEN -and -not $env:GITHUB_TOKEN) {
    try {
        $tok = gh auth token 2>$null
        if ($tok) { $env:GH_TOKEN = $tok }
    } catch { }
}

function Get-BashPath {
    $candidates = @(
        (Join-Path ${env:ProgramFiles} 'Git\bin\bash.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Git\bin\bash.exe'),
        'bash'
    )
    foreach ($c in $candidates) {
        if ($c -and (Test-Path $c -ErrorAction SilentlyContinue)) { return $c }
        if ($c -eq 'bash' -and (Get-Command bash -ErrorAction SilentlyContinue)) { return 'bash' }
    }
    return $null
}

# Prefer native PowerShell gh for auth-bound steps (WSL bash often lacks keyring).
if ($SetupAutomergeToken) {
    Write-Host '==> setup-automerge-token (PowerShell)'
    & "$PSScriptRoot\setup-automerge-token.ps1"
}

if ($RefreshGh) {
    Write-Host '==> ensure-gh-security-scope --refresh'
    $bash = Get-BashPath
    if (-not $bash) { throw 'Git Bash required for ensure-gh-security-scope.sh' }
    & $bash 'scripts/ensure-gh-security-scope.sh' '--refresh'
    if ($LASTEXITCODE -ne 0 -and $Strict) { exit $LASTEXITCODE }
}

# Probe Dependabot API with Windows gh
$repo = gh repo view --json nameWithOwner -q .nameWithOwner 2>$null
if ($repo) {
    gh api "repos/$repo/dependabot/alerts?state=open&per_page=1" 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "OK  gh:dependabot-alerts (API readable for $repo)"
    } else {
        Write-Host "LEFT gh:dependabot-alerts — run: .\scripts\automate-human-backlog.ps1 -RefreshGh"
        if ($Strict) { exit 1 }
    }
}

$bash = Get-BashPath
if (-not $bash) { throw 'Git Bash required for automate-human-backlog.sh' }

$args = @('scripts/automate-human-backlog.sh')
# Avoid double-running token setup inside bash when already done above
if ($SetupGithubRepo) { $args += '--setup-github-repo' }
if ($Strict) { $args += '--strict' }

& $bash @args
exit $LASTEXITCODE
