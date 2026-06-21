# Validates MSI via administrative extract (no elevation required) and optional libvips smoke.
param(
    [Parameter(Mandatory = $true)]
    [string]$MsiPath,
    [switch]$SmokeLibvips
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $MsiPath)) {
    throw "MSI not found: $MsiPath"
}

$extractRoot = Join-Path $env:TEMP ("qmi-msi-verify-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $extractRoot -Force | Out-Null

$msiFullPath = (Resolve-Path -LiteralPath $MsiPath).Path
$msiLength = (Get-Item -LiteralPath $msiFullPath).Length
if ($msiLength -lt 1024) {
    throw "MSI looks invalid (size ${msiLength} bytes): $msiFullPath"
}

try {
    Write-Host "Administrative extract to $extractRoot"
    $msiexecArgs = @(
        '/a',
        $msiFullPath,
        '/qn',
        "TARGETDIR=$extractRoot"
    )
    $proc = Start-Process -FilePath 'msiexec.exe' -ArgumentList $msiexecArgs -Wait -PassThru -NoNewWindow
    if ($proc.ExitCode -ne 0) {
        throw "msiexec /a failed with exit code $($proc.ExitCode)"
    }

    $exe = Get-ChildItem -Path $extractRoot -Recurse -Filter "QuickMediaIngest.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $exe) {
        throw "QuickMediaIngest.exe not found after administrative extract"
    }

    Write-Host "OK MSI extract: $($exe.FullName)"

    if ($SmokeLibvips) {
        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName = $exe.FullName
        $psi.Arguments = '--smoke-libvips'
        $psi.UseShellExecute = $false
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        $psi.CreateNoWindow = $true
        $smokeProc = [System.Diagnostics.Process]::Start($psi)
        if (-not $smokeProc) {
            throw 'Failed to start libvips smoke process'
        }
        $stdout = $smokeProc.StandardOutput.ReadToEnd()
        $stderr = $smokeProc.StandardError.ReadToEnd()
        if (-not $smokeProc.WaitForExit(120000)) {
            $smokeProc.Kill()
            throw 'libvips smoke timed out after 120s'
        }
        if ($smokeProc.ExitCode -ne 0 -or $stdout -notmatch 'OK libvips') {
            throw "libvips smoke failed (exit $($smokeProc.ExitCode)): stdout=$stdout stderr=$stderr"
        }
        Write-Host ($stdout.Trim())
        Write-Host 'OK libvips smoke on extracted MSI payload'
    }
}
finally {
    Remove-Item -Path $extractRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "MSI validation passed."
