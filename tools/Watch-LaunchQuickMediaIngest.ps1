#requires -Version 5.1
<#
.SYNOPSIS
  Launches QuickMediaIngest and streams quickmediaingest.log to the console until the app exits.

.DESCRIPTION
  Automates monitoring startup: tails %AppData%\QuickMediaIngest\quickmediaingest.log while the process runs.
  Stop with Ctrl+C (kills the app if still running) or close the app normally.

.PARAMETER ExePath
  Full path to QuickMediaIngest.exe. Defaults to publish\local-test next to repo root (this script's parent folder).

.PARAMETER LogPath
  Log file to tail. Defaults to %AppData%\QuickMediaIngest\quickmediaingest.log

.PARAMETER Configuration
  If ExePath is omitted, run via dotnet run with this configuration (default Release).

.EXAMPLE
  .\tools\Watch-LaunchQuickMediaIngest.ps1

.EXAMPLE
  .\tools\Watch-LaunchQuickMediaIngest.ps1 -ExePath "C:\path\to\QuickMediaIngest.exe"
#>
[CmdletBinding()]
param(
    [string] $ExePath = "",
    [string] $LogPath = "",
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release"
)

$ErrorActionPreference = "Stop"

# Script lives in repo\tools — repo root is one level up.
$repoRoot = Split-Path $PSScriptRoot -Parent
if (-not $repoRoot) { $repoRoot = (Get-Location).Path }

if (-not $LogPath) {
    $LogPath = Join-Path $env:APPDATA "QuickMediaIngest\quickmediaingest.log"
}

$defaultExe = Join-Path $repoRoot "publish\local-test\QuickMediaIngest.exe"
$csproj = Join-Path $repoRoot "QuickMediaIngest\QuickMediaIngest.csproj"

function Write-Banner([string] $msg) {
    Write-Host "`n=== $msg ===`n" -ForegroundColor Cyan
}

Write-Banner "QuickMediaIngest launch + log tail"
Write-Host "Log file: $LogPath"

$useDotnetRun = $false
if (-not $ExePath) {
    if (Test-Path $defaultExe) {
        $ExePath = $defaultExe
        Write-Host "EXE: $ExePath"
    }
    elseif (Test-Path $csproj) {
        $useDotnetRun = $true
        Write-Host "Using: dotnet run -c $Configuration (no publish\local-test EXE found)"
    }
    else {
        throw "Could not find QuickMediaIngest.exe or project. Set -ExePath explicitly."
    }
}
else {
    Write-Host "EXE: $ExePath"
}

$tailScript = {
    param([string] $path)
    $dir = Split-Path $path -Parent
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    $deadline = (Get-Date).AddSeconds(120)
    while (-not (Test-Path $path) -and (Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 250
    }
    if (-not (Test-Path $path)) {
        Write-Host "[watch] Log file not created within 120s; still waiting for lines..." -ForegroundColor Yellow
        while (-not (Test-Path $path)) { Start-Sleep -Milliseconds 500 }
    }
    Get-Content -Path $path -Tail 80 -ErrorAction SilentlyContinue
    Get-Content -Path $path -Wait -Tail 10 -ErrorAction Stop
}

$job = Start-Job -ScriptBlock $tailScript -ArgumentList $LogPath

try {
    if ($useDotnetRun) {
        $projDir = Split-Path $csproj -Parent
        $proc = Start-Process -FilePath "dotnet" `
            -ArgumentList @("run", "-c", $Configuration, "--project", $csproj) `
            -PassThru -WorkingDirectory $projDir
    }
    else {
        $proc = Start-Process -FilePath $ExePath -WorkingDirectory (Split-Path $ExePath -Parent) -PassThru
    }

    Write-Banner "Log output (live). Close the app or press Ctrl+C to stop."
    while (-not $proc.HasExited) {
        Receive-Job -Job $job | ForEach-Object { $_ }
        Start-Sleep -Milliseconds 300
    }
    Receive-Job -Job $job | ForEach-Object { $_ }
    Write-Host "`nProcess exited with code $($proc.ExitCode)." -ForegroundColor Green
}
catch {
    Write-Host $_ -ForegroundColor Red
}
finally {
    if ($job -and ($job.State -eq "Running" -or $job.State -eq "NotStarted")) {
        Stop-Job -Job $job -Force -ErrorAction SilentlyContinue
    }
    Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
}
