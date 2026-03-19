$csproj = "QuickMediaIngest/QuickMediaIngest.csproj"

if (Test-Path $csproj) {
    $content = Get-Content $csproj -Raw
    $v = [regex]::Match($content, '<Version>(.*)</Version>').Groups[1].Value
    if (-not $v) { $v = "1.0.0" }
}
else {
    $v = "1.0.0"
}

$v = $v.Trim()
$p = $v.Split('.')

if ($p.Count -ne 3) {
    $v = "1.0.0"
    $p = $v.Split('.')
}

$patch = [string]::Format('{0}.{1}.{2}', $p[0], $p[1], ([int]$p[2] + 1))
$minor = [string]::Format('{0}.{1}.0', $p[0], ([int]$p[1] + 1))
$major = [string]::Format('{0}.0.0', ([int]$p[0] + 1))

# Industry-standard semver guidance:
# - major for explicit breaking change markers
# - minor for new features (feat commits) or notable app-surface additions
# - patch for fixes/chore/docs-only changes
$recommendedVersion = $patch
$bumpType = "patch"
$reason = "Default patch bump for compatibility-safe updates."

$hasGit = $null -ne (Get-Command git -ErrorAction SilentlyContinue)
if ($hasGit) {
    $logText = ""
    $diffFiles = @()
    $nameStatus = @()

    $lastTag = git describe --tags --abbrev=0 2>$null
    if ($LASTEXITCODE -eq 0 -and $lastTag) {
        $logText = (git log "$lastTag..HEAD" --pretty=format:%s`n%b 2>$null) -join "`n"
    }
    else {
        $logText = (git log --max-count=30 --pretty=format:%s`n%b 2>$null) -join "`n"
    }

    $diffFiles = @(git diff --name-only HEAD 2>$null)
    $nameStatus = @(git diff --name-status HEAD 2>$null)

    $hasBreaking = $logText -match '(?im)BREAKING CHANGE|^.+!:'
    $hasFeatCommit = $logText -match '(?im)^feat(\(.+\))?:\s'
    $hasAppCodeChange = $diffFiles | Where-Object { $_ -match '^QuickMediaIngest/' }
    $addedAppFile = $nameStatus | Where-Object {
        $_ -match '^A\s+QuickMediaIngest/'
    }

    if ($hasBreaking) {
        $recommendedVersion = $major
        $bumpType = "major"
        $reason = "Detected explicit breaking-change markers in commit history."
    }
    elseif ($hasFeatCommit -or $addedAppFile) {
        $recommendedVersion = $minor
        $bumpType = "minor"
        $reason = "Detected new feature-level change (feat commit or new app surface)."
    }
    elseif ($hasAppCodeChange) {
        $recommendedVersion = $patch
        $bumpType = "patch"
        $reason = "Detected code changes without breaking/feature markers."
    }
    else {
        $recommendedVersion = $patch
        $bumpType = "patch"
        $reason = "Detected docs/tooling-only changes; patch is sufficient."
    }
}

$reason = $reason -replace '\|', '/'
Write-Output "$v|$patch|$minor|$major|$recommendedVersion|$bumpType|$reason"
