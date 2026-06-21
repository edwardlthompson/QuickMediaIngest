#Requires -Version 5.1
<#
.SYNOPSIS
  Run local quality gates on Windows (mirrors CI bash scripts).

.EXAMPLE
  .\scripts\validate-local.ps1
  .\scripts\validate-local.ps1 -SkipFormat
  .\scripts\validate-local.ps1 -WatchGates
  .\scripts\validate-local.ps1 -PreRelease
#>
param(
    [switch]$SkipFormat,
    [switch]$SkipBuild,
    [switch]$WatchGates,
    [switch]$PreRelease,
    [switch]$QuickBootstrap,
    [switch]$SmokeHuman
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

function Test-BashAvailable {
    return [bool](Get-BashPath)
}

function Invoke-BashGate {
    param([string]$Name, [string]$Script, [string[]]$Args = @())
    $bash = Get-BashPath
    if (-not $bash) {
        throw "$Name requires Git Bash (install Git for Windows). WSL is not configured on this machine."
    }
    Write-Host "==> $Name"
    & $bash $Script @Args
    if ($LASTEXITCODE -ne 0) { throw "$Name failed (exit $LASTEXITCODE)" }
}

function Invoke-DotnetFeatureGate {
    Write-Host '==> dotnet feature gate (native fallback)'
    dotnet restore QuickMediaIngest-1.sln
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed' }
    dotnet build QuickMediaIngest-1.sln -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed' }
    dotnet test QuickMediaIngest-1.sln -c Release --no-build --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw 'dotnet test failed' }
    if (-not $SkipFormat) {
        dotnet format QuickMediaIngest-1.sln --verify-no-changes
        if ($LASTEXITCODE -ne 0) { throw 'dotnet format check failed' }
    }
    dotnet list QuickMediaIngest-1.sln package --vulnerable --include-transitive
    if ($LASTEXITCODE -ne 0) { throw 'Vulnerable packages detected' }
}

$bootstrapArgs = @()
if ($QuickBootstrap) { $bootstrapArgs += '--quick' }

Invoke-BashGate 'Bootstrap artifacts' 'scripts/validate-bootstrap.sh' $bootstrapArgs
Invoke-BashGate 'Batch commands' 'scripts/check-batch-commands.sh'
Invoke-BashGate 'Repo hygiene' 'scripts/check-repo-hygiene.sh'
Invoke-BashGate 'File line limits' 'scripts/check-file-limits.sh'
Invoke-BashGate 'UTF-8 encoding' 'scripts/check-file-encoding.sh'
Invoke-BashGate 'License compliance' 'scripts/check-license-compliance.sh'
Invoke-BashGate 'Template index' 'scripts/validate-template-index.sh'

if ($WatchGates) {
    if (Test-BashAvailable) {
        Invoke-BashGate 'Watch agent gates' 'scripts/watch-agent-gates.sh' @('--once', '--autofix')
    } else {
        Write-Host '==> Watch agent gates (bash unavailable — dotnet fallback)'
        Invoke-DotnetFeatureGate
    }
}

if ($PreRelease) {
    Invoke-BashGate 'Pre-release gate' 'scripts/pre-release-gate.sh'
}

if (-not $SkipBuild) {
    if (Test-BashAvailable) {
        Invoke-BashGate 'Feature gate' 'scripts/feature-gate.sh' @('--stack', 'dotnet-wpf')
    } else {
        Invoke-DotnetFeatureGate
    }
}

if ($SmokeHuman) {
    Write-Host '==> Human verification smoke'
    & $PSScriptRoot/smoke-human-verification.ps1 -SkipSecurityTriage:(-not (Get-Command gh -ErrorAction SilentlyContinue))
    if ($LASTEXITCODE -ne 0) { throw 'Human verification smoke failed' }
}

Write-Host 'All local gates passed.'
