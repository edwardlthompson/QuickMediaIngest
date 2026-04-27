#requires -Version 5.1
<#
.SYNOPSIS
  Launches QuickMediaIngest and streams AppData logs until the app process exits.

.DESCRIPTION
  - Tails quickmediaingest.log (incremental bytes) with session start time filtering.
  - Watches fatal.log and %AppData%\QuickMediaIngest\logs\crash_*.log for new writes.
  - Highlights: startup, unified, FTP scan, local scan, ingest WallTimeMs, Import finished, errors.
  - Stops when the launched dotnet/app process exits (you closed the app).

.PARAMETER Configuration
  dotnet -c value (default Release).

.PARAMETER MaxWatchMinutes
  Stop streaming after this many minutes even if the app is still running (0 = unlimited).

.EXAMPLE
  .\tools\Monitor-AppSession.ps1

.EXAMPLE
  .\tools\Monitor-AppSession.ps1 -MaxWatchMinutes 15
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [ValidateRange(0, 1440)]
    [int] $MaxWatchMinutes = 0
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$proj = Join-Path $repoRoot 'QuickMediaIngest\QuickMediaIngest.csproj'
$wd = Split-Path $proj -Parent
$baseDir = Join-Path $env:APPDATA 'QuickMediaIngest'
$logPath = Join-Path $baseDir 'quickmediaingest.log'
$fatalPath = Join-Path $baseDir 'fatal.log'
$crashDir = Join-Path $baseDir 'logs'

Write-Host "`n=== QuickMediaIngest session monitor ===" -ForegroundColor Cyan
Write-Host "Main log:    $logPath"
Write-Host "Fatal log:   $fatalPath"
Write-Host "Crash logs:  $crashDir`n"

Write-Host 'Launching app (dotnet run)...' -ForegroundColor Yellow
$proc = Start-Process -FilePath 'dotnet' `
    -ArgumentList @('run', '-c', $Configuration, '--project', $proj) `
    -WorkingDirectory $wd -PassThru -WindowStyle Normal

Start-Sleep -Milliseconds 900
$sessionStart = [DateTimeOffset]::UtcNow
if ($MaxWatchMinutes -gt 0) {
    Write-Host "[monitor] Session start (UTC): $sessionStart - streaming PID $($proc.Id) until exit or $MaxWatchMinutes min cap.`n" -ForegroundColor Green
}
else {
    Write-Host "[monitor] Session start (UTC): $sessionStart - streaming until process PID $($proc.Id) exits.`n" -ForegroundColor Green
}

$watchDeadline = $null
if ($MaxWatchMinutes -gt 0) {
    $watchDeadline = (Get-Date).AddMinutes($MaxWatchMinutes)
}
Write-Host "Unified note: sidebar lists removable drives by default; fixed drives must be enabled in drive selection. Unified merges only those sources + FTP.`n" -ForegroundColor DarkGray

function Read-NewBytes([string] $path, [ref] $offset) {
    if (-not (Test-Path $path)) { return '' }
    $fi = Get-Item $path
    if ($fi.Length -lt $offset.Value) { $offset.Value = 0 }
    if ($fi.Length -le $offset.Value) { return '' }
    $fs = [System.IO.File]::Open($path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
    try {
        $fs.Position = $offset.Value
        $len = [int64]($fi.Length - $offset.Value)
        $buf = New-Object byte[] $len
        [void]$fs.Read($buf, 0, $len)
        $offset.Value = $fi.Length
        return [System.Text.Encoding]::UTF8.GetString($buf)
    }
    finally {
        $fs.Close()
    }
}

function Split-Lines([string] $chunk) {
    if ([string]::IsNullOrEmpty($chunk)) { return @() }
    return $chunk -split "`r?`n"
}

function Color-Line([string] $line) {
    if ($line -match '\[Error\]') {
        Write-Host $line -ForegroundColor Red
    }
    elseif ($line -match '\[Warning\]') {
        Write-Host $line -ForegroundColor Yellow
    }
    elseif ($line -match 'Application startup (initiated|completed)|Unified load starting|Unified load skipped|Unified source load failed|Starting FTP scan|Completed FTP scan|Starting local scan|Completed local scan|Starting ingest for group|Completed ingest for group|Import started\.|Import finished\.|fatal|crash|Unhandled') {
        Write-Host $line -ForegroundColor Cyan
    }
    else {
        Write-Host $line
    }
}

$offMain = 0L
if (Test-Path $logPath) { $offMain = (Get-Item $logPath).Length }

$offFatal = 0L
if (Test-Path $fatalPath) { $offFatal = (Get-Item $fatalPath).Length }

$seenCrash = New-Object 'System.Collections.Generic.HashSet[string]'

$timedOut = $false
while (-not $proc.HasExited) {
    if ($null -ne $watchDeadline -and (Get-Date) -ge $watchDeadline) {
        $timedOut = $true
        Write-Host "`n[monitor] Max watch time ($MaxWatchMinutes min) reached; stopping stream (app left running).`n" -ForegroundColor Yellow
        break
    }
    try {
        $chunk = Read-NewBytes $logPath ([ref]$offMain)
        foreach ($ln in (Split-Lines $chunk)) {
            if ([string]::IsNullOrWhiteSpace($ln)) { continue }
            try {
                $iso = ($ln -split '\s')[0]
                $ts = [DateTimeOffset]::Parse($iso)
                if ($ts -lt $sessionStart) { continue }
            }
            catch { }

            Color-Line $ln
        }

        $fatalChunk = Read-NewBytes $fatalPath ([ref]$offFatal)
        foreach ($ln in (Split-Lines $fatalChunk)) {
            if ([string]::IsNullOrWhiteSpace($ln)) { continue }
            Write-Host "[FATAL] $ln" -ForegroundColor Red
        }

        if (Test-Path $crashDir) {
            Get-ChildItem -Path $crashDir -Filter 'crash_*.log' -ErrorAction SilentlyContinue |
                Where-Object { $_.LastWriteTimeUtc -ge $sessionStart.UtcDateTime.AddSeconds(-2) } |
                ForEach-Object {
                    if (-not $seenCrash.Contains($_.FullName)) {
                        [void]$seenCrash.Add($_.FullName)
                        Write-Host "`n*** NEW CRASH FILE: $($_.FullName) ***" -ForegroundColor Magenta
                        Get-Content $_.FullName -ErrorAction SilentlyContinue | ForEach-Object { Write-Host $_ -ForegroundColor Magenta }
                        Write-Host ''
                    }
                }
        }
    }
    catch {
        Write-Host "[monitor read error] $_" -ForegroundColor DarkYellow
    }

    Start-Sleep -Milliseconds 400
}

if (-not $timedOut) {
    Wait-Process -Id $proc.Id -ErrorAction SilentlyContinue
    $exit = $proc.ExitCode
    Write-Host "`n=== Process exited (exit code $exit). Session summary ===" -ForegroundColor Green
}
else {
    Write-Host "`n=== Session summary (partial; watch ended by time cap) ===" -ForegroundColor Yellow
}

if (Test-Path $logPath) {
    $sessionLines = foreach ($ln in (Get-Content $logPath -Tail 8000 -ErrorAction SilentlyContinue)) {
        if ($ln -notmatch '^(\d{4}-\d{2}-\d{2}T[\d:\.+-]+)') { continue }
        try {
            $ts = [DateTimeOffset]::Parse($Matches[1])
            if ($ts -ge $sessionStart) { $ln }
        }
        catch { }
    }

    $startupInit = @($sessionLines) | Where-Object { $_ -match 'Application startup initiated' } | Select-Object -First 1
    $startupDone = @($sessionLines) | Where-Object { $_ -match 'Application startup completed' } | Select-Object -First 1
    if ($startupInit -and $startupDone) {
        try {
            $t1 = [DateTimeOffset]::Parse(($startupInit -split '\s')[0])
            $t2 = [DateTimeOffset]::Parse(($startupDone -split '\s')[0])
            Write-Host ("Startup UI path: {0:N0} ms (initiated -> completed)" -f ($t2 - $t1).TotalMilliseconds)
        }
        catch { }
    }

    Write-Host "`nKey lines this session:"
    @($sessionLines) |
        Where-Object {
            $_ -match 'Unified load starting|Unified load skipped|Unified source load failed|Error loading Unified|Starting FTP scan|Completed FTP scan|Starting local scan|Completed local scan|Starting ingest for group|Completed ingest for group|Import started\.|Import finished\.|\[Error\]'
        } |
        Select-Object -First 80 |
        ForEach-Object { Write-Host $_ }
}

Write-Host "`n[monitor] Done." -ForegroundColor Green
