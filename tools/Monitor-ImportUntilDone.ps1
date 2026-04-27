#requires -Version 5.1
param(
    [int] $TimeoutMinutes = 12
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$proj = Join-Path $repoRoot 'QuickMediaIngest\QuickMediaIngest.csproj'
$wd = Split-Path $proj -Parent
$log = Join-Path $env:APPDATA 'QuickMediaIngest\quickmediaingest.log'
$t0 = [DateTimeOffset]::UtcNow

Write-Host '[monitor] Launching QuickMediaIngest (dotnet run -c Release)...'
Start-Process -FilePath 'dotnet' `
    -ArgumentList @('run', '-c', 'Release', '--project', $proj) `
    -WorkingDirectory $wd -WindowStyle Normal

Write-Host "[monitor] Log: $log"
Write-Host "[monitor] Waiting up to $TimeoutMinutes min for 'Import finished' (complete an import in the app)."

$deadline = (Get-Date).AddMinutes($TimeoutMinutes)
$found = $false
while ((Get-Date) -lt $deadline -and -not $found) {
    Start-Sleep -Seconds 4
    if (-not (Test-Path $log)) { continue }
    $lines = Get-Content $log -Tail 800 -ErrorAction SilentlyContinue
    foreach ($line in $lines) {
        if ($line -notmatch 'Import finished') { continue }
        try {
            $iso = ($line -split '\s')[0]
            $lt = [DateTimeOffset]::Parse($iso)
            if ($lt -ge $t0.AddSeconds(-30)) { $found = $true; break }
        }
        catch {
            $found = $true
            break
        }
    }
}

if (-not $found) {
    Write-Host '[monitor] TIMEOUT: no recent Import finished. Last 100 log lines:'
    if (Test-Path $log) { Get-Content $log -Tail 100 -ErrorAction SilentlyContinue }
    exit 2
}

Write-Host ''
Write-Host '=== Ingest / import timeline (recent matches) ==='
Select-String -Path $log -Pattern 'Application startup (initiated|completed)\.|Import started\.|Starting ingest for group|Completed ingest for group|Import finished\.' -ErrorAction SilentlyContinue |
    Select-Object -Last 40 | ForEach-Object { $_.Line }

Write-Host ''
Write-Host '[monitor] Done.'
