#Requires -Version 5.1
<#
.SYNOPSIS
  Automated smoke for BUILD_PLAN HUMAN verification rows.

.EXAMPLE
  .\scripts\smoke-human-verification.ps1
  .\scripts\smoke-human-verification.ps1 -RequireFtp
  .\scripts\smoke-human-verification.ps1 -SkipSecurityTriage
#>
param(
    [switch]$RequireFtp,
    [switch]$SkipSecurityTriage
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

if ($RequireFtp) {
    $env:QMI_SMOKE_REQUIRE = '1'
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

Write-Host '==> Human verification smoke (config + optional LAN FTP)'

$filter = 'FullyQualifiedName~ConfigFilePersistenceTests|FullyQualifiedName~HumanVerificationSmokeTests|FullyQualifiedName~FtpThumbnail'
dotnet restore QuickMediaIngest-1.sln
dotnet build QuickMediaIngest-1.sln -c Release --no-restore
dotnet test QuickMediaIngest-1.sln -c Release --no-build --filter $filter --verbosity normal
if ($LASTEXITCODE -ne 0) { throw 'Human verification smoke tests failed' }

if (-not $SkipSecurityTriage) {
    $bash = Get-BashPath
    if ($bash -and (Get-Command gh -ErrorAction SilentlyContinue)) {
        Write-Host '==> Security triage (automated; non-fatal when Dependabot scope missing)'
        & $bash scripts/check-security-triage.sh
        if ($LASTEXITCODE -ne 0) {
            Write-Warning 'Security triage incomplete — run gh auth refresh -s security_events or see docs/SECURITY_TRIAGE.md'
        }
    }
    else {
        Write-Warning 'SKIP: security triage (Git Bash or gh CLI unavailable)'
    }
}

Write-Host 'Human verification smoke passed.'
