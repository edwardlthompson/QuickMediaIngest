#requires -Version 5.1
<#
.SYNOPSIS
  Launches QuickMediaIngest and monitors quickmediaingest.log for FTP scan, FTP activity, and import completion.

.DESCRIPTION
  Waits until (1) an Import finished line appears after launch and (2) FTP was used in that window
  (FTP scan completed and/or FTP file provider connected for copy). Parses wall times between
  Starting FTP scan to Completed FTP scan when both appear.

  You must in the GUI: select an FTP or Unified source, refresh/scan, optionally wait for thumbnails,
  then run an import that includes files from FTP.

.PARAMETER TimeoutMinutes
  Max wait time (default 25).
#>
param(
    [int] $TimeoutMinutes = 25
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$proj = Join-Path $repoRoot 'QuickMediaIngest\QuickMediaIngest.csproj'
$wd = Split-Path $proj -Parent
$log = Join-Path $env:APPDATA 'QuickMediaIngest\quickmediaingest.log'

Write-Host '[ftp-monitor] Launching QuickMediaIngest (dotnet run -c Release)...'
Start-Process -FilePath 'dotnet' `
    -ArgumentList @('run', '-c', 'Release', '--project', $proj) `
    -WorkingDirectory $wd -WindowStyle Normal

# Only count log lines written after launch (avoid matching a prior Import finished still in the tail).
Start-Sleep -Milliseconds 800
$t0 = [DateTimeOffset]::UtcNow

Write-Host "[ftp-monitor] Log: $log"
Write-Host '[ftp-monitor] In the app: connect FTP or Unified, scan, optional previews, then import FTP files.'
Write-Host "[ftp-monitor] Waiting up to $TimeoutMinutes min for FTP activity + Import finished...`n"

$deadline = (Get-Date).AddMinutes($TimeoutMinutes)
$sawFtpScan = $false
$sawFtpComplete = $false
$sawFtpProvider = $false
$sawImportStart = $false
$sawImportDone = $false
$scanStartLines = @()
$scanEndLines = @()

function Test-IsoAfterSession {
    param([string] $Line)
    if ($Line -notmatch '^(\d{4}-\d{2}-\d{2}T[\d:\.+-]+)') { return $false }
    try {
        $ts = [DateTimeOffset]::Parse($Matches[1])
        return $ts -ge $t0
    }
    catch { return $false }
}

while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 3
    if (-not (Test-Path $log)) { continue }

    $lines = Get-Content $log -Tail 2500 -ErrorAction SilentlyContinue
    foreach ($line in $lines) {
        if (-not (Test-IsoAfterSession $line)) { continue }

        if ($line -match 'Starting FTP scan for ') {
            $sawFtpScan = $true
            if ($scanStartLines -notcontains $line) { $scanStartLines += $line }
        }
        if ($line -match 'Completed FTP scan for ') {
            $sawFtpComplete = $true
            if ($scanEndLines -notcontains $line) { $scanEndLines += $line }
        }
        if ($line -match 'Connected FTP file provider to ') { $sawFtpProvider = $true }
        if ($line -match 'Import started\.') { $sawImportStart = $true }
        if ($line -match 'Import finished\.') { $sawImportDone = $true }
    }

    $ftpEvidence = $sawFtpScan -or $sawFtpComplete -or $sawFtpProvider
    if ($sawImportDone -and $ftpEvidence) { break }
}

Write-Host '=== Session markers (since launch, from tail scan) ==='
Write-Host "  FTP scan started:    $sawFtpScan"
Write-Host "  FTP scan completed:  $sawFtpComplete"
Write-Host "  FTP provider (copy): $sawFtpProvider"
Write-Host "  Import started:      $sawImportStart"
Write-Host "  Import finished:       $sawImportDone"

if (-not $sawImportDone -or -not ($sawFtpScan -or $sawFtpComplete -or $sawFtpProvider)) {
    Write-Host "`n[ftp-monitor] Incomplete: need Import finished plus FTP scan and/or FTP file provider activity."
    Get-Content $log -Tail 120 -ErrorAction SilentlyContinue
    exit 2
}

Write-Host "`n=== FTP scan timing (first complete pair in session lines) ==="
try {
    $startPat = 'Starting FTP scan for (?<rest>.+)\. IncludeSubfolders=(?<sub>.+)$'
    $endPat = 'Completed FTP scan for (?<rest>.+)\. Files=(?<files>\d+), Folders=(?<folders>\d+), SkippedFolders=(?<skip>\d+)'

    $pairFound = $false
    foreach ($sl in $scanStartLines) {
        if ($sl -notmatch $startPat) { continue }
        $key = $Matches['rest']
        $tStart = [DateTimeOffset]::Parse(($sl -split '\s')[0])

        foreach ($el in $scanEndLines) {
            if ($el -notmatch $endPat) { continue }
            if ($Matches['rest'] -ne $key) { continue }
            $tEnd = [DateTimeOffset]::Parse(($el -split '\s')[0])
            $dur = ($tEnd - $tStart).TotalSeconds
            Write-Host ("  Scan wall time: {0:N2}s | {1}" -f $dur, $key)
            Write-Host ("    Files={0}, Folders={1}, SkippedFolders={2}" -f $Matches['files'], $Matches['folders'], $Matches['skip'])
            $pairFound = $true
            break
        }
        if ($pairFound) { break }
    }
    if (-not $pairFound) { Write-Host '  (Could not pair start/end lines - check log manually.)' }
}
catch {
    Write-Host "  Parse error: $_"
}

Write-Host "`n=== Recent ingest / FTP copy lines (Information) ==="
Select-String -Path $log -Pattern 'Starting ingest for group|Completed ingest for group|Connected FTP file provider|Starting FTP scan|Completed FTP scan|Import started\.|Import finished\.' -ErrorAction SilentlyContinue |
    Select-Object -Last 35 | ForEach-Object { $_.Line }

Write-Host "`n[ftp-monitor] Done."
