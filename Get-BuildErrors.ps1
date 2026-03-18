# Get Latest Failed Run ID
$run = gh run list --limit 1 | Out-String
$runId = $null

if ($run -match "failure\s+.*\s+(\d{10,12})\s+") {
    $runId = $Matches[1]
}

if ($runId) {
    Write-Host "Fetching errors for run $runId..."
    $logs = gh run view $runId --log | Out-String
    $errors = $logs -split "`n" | Where-Object { $_ -match "##\[error\]" }
    
    if ($errors) {
        $report = @(
            "# GitHub Build Error Report",
            "**Run ID**: $runId",
            "",
            "## Errors Found",
            "```"
        )
        $report += $errors
        $report += "```"
        
        $report | Out-File -FilePath "build_errors.md" -Encoding utf8
        Write-Host "Errors saved to build_errors.md successfully!"
    } else {
        Write-Host "No errors with type 'error' found in the failure log."
    }
} else {
    Write-Host "No failed run found in the latest item."
}
 drum
(Wait, REMOVE DRUM, absolute target content replacement)
