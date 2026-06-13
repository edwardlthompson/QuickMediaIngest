#Requires -Version 5.1
<#
.SYNOPSIS
  Run local quality gates on Windows (mirrors CI bash scripts).

.EXAMPLE
  .\scripts\validate-local.ps1
  .\scripts\validate-local.ps1 -SkipFormat
#>
param(
    [switch]$SkipFormat,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

function Invoke-BashGate {
    param([string]$Name, [string]$Script, [string[]]$Args = @())
    Write-Host "==> $Name"
    & bash $Script @Args
    if ($LASTEXITCODE -ne 0) { throw "$Name failed (exit $LASTEXITCODE)" }
}

Invoke-BashGate 'Bootstrap artifacts' 'scripts/validate-bootstrap.sh'
Invoke-BashGate 'File line limits' 'scripts/check-file-limits.sh'
Invoke-BashGate 'UTF-8 encoding' 'scripts/check-file-encoding.sh'
Invoke-BashGate 'License compliance' 'scripts/check-license-compliance.sh'
Invoke-BashGate 'Template index' 'scripts/validate-template-index.sh'

if (-not $SkipBuild) {
    Write-Host '==> dotnet restore'
    dotnet restore QuickMediaIngest-1.sln
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed' }

    Write-Host '==> dotnet build (Release)'
    dotnet build QuickMediaIngest-1.sln -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed' }

    Write-Host '==> dotnet test (Release)'
    dotnet test QuickMediaIngest-1.sln -c Release --no-build --verbosity normal
    if ($LASTEXITCODE -ne 0) { throw 'dotnet test failed' }

    Write-Host '==> Vulnerable packages'
    dotnet list QuickMediaIngest-1.sln package --vulnerable --include-transitive
    if ($LASTEXITCODE -ne 0) { throw 'Vulnerable packages detected' }
}

if (-not $SkipFormat) {
    Write-Host '==> dotnet format --verify-no-changes'
    dotnet format QuickMediaIngest-1.sln --verify-no-changes
    if ($LASTEXITCODE -ne 0) { throw 'dotnet format check failed' }
}

Write-Host 'All local gates passed.'
