#Requires -Version 5.1
<#
.SYNOPSIS
  Smoke-test published portable exe (libvips native DLL bundle).

.EXAMPLE
  .\scripts\smoke-published-exe.ps1
  .\scripts\smoke-published-exe.ps1 -Rebuild
  .\scripts\smoke-published-exe.ps1 -RequirePublished
#>
param(
    [switch]$Rebuild,
    [switch]$RequirePublished
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

$exe = Join-Path $Root 'publish\local-test\QuickMediaIngest.exe'

if ($Rebuild -or -not (Test-Path $exe)) {
    Write-Host '==> Building published portable exe (build_local_test.bat)'
    cmd /c build_local_test.bat
    if ($LASTEXITCODE -ne 0) { throw 'build_local_test.bat failed' }
}

if (-not (Test-Path $exe)) {
    if ($RequirePublished) {
        throw "Published exe missing: $exe"
    }

    Write-Warning "SKIP: published exe not found at $exe"
    exit 0
}

Write-Host "==> Headless libvips smoke: $exe --smoke-libvips"
$env:QMI_SMOKE_PUBLISHED_EXE = $exe
if ($RequirePublished) {
    $env:QMI_SMOKE_REQUIRE_PUBLISHED = '1'
}

& $exe --smoke-libvips
if ($LASTEXITCODE -ne 0) {
    throw "Published exe --smoke-libvips failed (exit $LASTEXITCODE)"
}

Write-Host '==> PublishedExeSmokeTests (dotnet)'
dotnet test QuickMediaIngest-1.sln -c Release --filter FullyQualifiedName~PublishedExeSmokeTests --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw 'PublishedExeSmokeTests failed' }

Write-Host 'Published exe smoke passed.'
