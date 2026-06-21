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

try {
    Write-Host "Administrative extract to $extractRoot"
    $args = @("/a", "`"$MsiPath`"", "/qn", "TARGETDIR=`"$extractRoot`"")
    $proc = Start-Process -FilePath "msiexec.exe" -ArgumentList $args -Wait -PassThru -NoNewWindow
    if ($proc.ExitCode -ne 0) {
        throw "msiexec /a failed with exit code $($proc.ExitCode)"
    }

    $exe = Get-ChildItem -Path $extractRoot -Recurse -Filter "QuickMediaIngest.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $exe) {
        throw "QuickMediaIngest.exe not found after administrative extract"
    }

    Write-Host "OK MSI extract: $($exe.FullName)"

    if ($SmokeLibvips) {
        & $exe.FullName --smoke-libvips
        if ($LASTEXITCODE -ne 0) {
            throw "libvips smoke failed with exit code $LASTEXITCODE"
        }
        Write-Host "OK libvips smoke on extracted MSI payload"
    }
}
finally {
    Remove-Item -Path $extractRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "MSI validation passed."
