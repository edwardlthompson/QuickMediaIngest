$csproj = "QuickMediaIngest/QuickMediaIngest.csproj"
if (Test-Path $csproj) {
    $content = Get-Content $csproj -Raw
    $v = [regex]::Match($content, '<Version>(.*)</Version>').Groups[1].Value
    if (-not $v) { $v = "1.0.0" }
} else {
    $v = "1.0.0"
}

$v = $v.Trim()
$p = $v.Split('.')

if ($p.Count -eq 3) {
    $patch = [string]::Format('{0}.{1}.{2}', $p[0], $p[1], ([int]$p[2] + 1))
    $minor = [string]::Format('{0}.{1}.0', $p[0], ([int]$p[1] + 1))
    $major = [string]::Format('{0}.0.0', ([int]$p[0] + 1))
    echo "$v|$patch|$minor|$major"
} else {
    echo "$v|1.0.1|1.1.0|2.0.0"
}
