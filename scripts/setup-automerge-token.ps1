#Requires -Version 5.1
<#
.SYNOPSIS
  Set repo secret AUTOMERGE_TOKEN so Dependabot merges trigger push CI.

.EXAMPLE
  .\scripts\setup-automerge-token.ps1
  $env:AUTOMERGE_TOKEN = 'ghp_...'; .\scripts\setup-automerge-token.ps1
#>
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw 'gh CLI required (https://cli.github.com/)'
}

$token = $env:AUTOMERGE_TOKEN
if (-not $token) {
    $token = (gh auth token 2>$null)
}
if (-not $token) {
    throw 'Set AUTOMERGE_TOKEN env or run gh auth login'
}

# gh secret set reads body from stdin when --body is omitted on some versions; use --body
gh secret set AUTOMERGE_TOKEN --body $token
$repo = gh repo view --json nameWithOwner -q .nameWithOwner
Write-Host "OK   AUTOMERGE_TOKEN set on $repo"
Write-Host 'Note: classic PATs with workflow scope, or fine-grained tokens with Contents+Workflows, work best.'
