#Requires -Version 5.1
<#
.SYNOPSIS
  Automate BUILD_PLAN HUMAN sign-off items (tests, smoke, security triage, optional push).

.EXAMPLE
  .\scripts\run-human-signoffs.ps1
  .\scripts\run-human-signoffs.ps1 -Strict -WaitCi 300
  .\scripts\run-human-signoffs.ps1 -RefreshGh -PublishedExe
  $env:QMI_ALLOW_PUSH = '1'; .\scripts\run-human-signoffs.ps1 -Push
#>
param(
    [switch]$Strict,
    [int]$WaitCi = 0,
    [switch]$RefreshGh,
    [switch]$PublishedExe,
    [switch]$Push,
    [switch]$SkipSecurity
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

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

$bash = Get-BashPath
if (-not $bash) { throw 'Git Bash required for run-human-signoffs.ps1' }

$args = @('scripts/run-human-signoffs.sh')
if ($Strict) { $args += '--strict' }
if ($WaitCi -gt 0) { $args += '--wait-ci'; $args += "$WaitCi" }
if ($RefreshGh) { $args += '--refresh-gh' }
if ($PublishedExe) { $args += '--published-exe' }
if ($Push) { $args += '--push' }
if ($SkipSecurity) { $args += '--skip-security' }

& $bash @args
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
