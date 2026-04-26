# create_icon.ps1
# Builds the multi-resolution QuickMediaIngest.ico from the canonical
# PNG source (Assets/AppIcon.png).
#
# Run:   powershell -ExecutionPolicy Bypass -File create_icon.ps1
#
# Inputs:
#   QuickMediaIngest/Assets/AppIcon.png         (preferred, square)
#   QuickMediaIngest/Assets/AppIcon.source.png  (raw export, may be wider; auto-cropped)
#
# Output:
#   QuickMediaIngest/QuickMediaIngest.ico       (PNG-in-ICO, 16/32/48/64/128/256)

Add-Type -AssemblyName System.Drawing

$ProjectDir   = Join-Path $PSScriptRoot "QuickMediaIngest"
$AssetsDir    = Join-Path $ProjectDir   "Assets"
$IconPng      = Join-Path $AssetsDir    "AppIcon.png"
$IconSrcPng   = Join-Path $AssetsDir    "AppIcon.source.png"
$OutputIco    = Join-Path $ProjectDir   "QuickMediaIngest.ico"

# ── Ensure we have a square canonical PNG ────────────────────────────────────
function Ensure-SquareCanonical {
    if (-not (Test-Path $IconPng) -and -not (Test-Path $IconSrcPng)) {
        throw "No source PNG found. Expected $IconPng or $IconSrcPng."
    }

    if (Test-Path $IconPng) {
        $probe = [System.Drawing.Image]::FromFile($IconPng)
        $isSquare = ($probe.Width -eq $probe.Height)
        $probe.Dispose()
        if ($isSquare) { return }
    }

    $sourcePath = if (Test-Path $IconSrcPng) { $IconSrcPng } else { $IconPng }
    Write-Host "Normalizing $sourcePath into square 1024x1024 -> $IconPng"

    $src = [System.Drawing.Image]::FromFile($sourcePath)
    try {
        $side   = [Math]::Min($src.Width, $src.Height)
        $offX   = [int](($src.Width  - $side) / 2)
        $offY   = [int](($src.Height - $side) / 2)
        $crop   = New-Object System.Drawing.Bitmap $side, $side, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g      = [System.Drawing.Graphics]::FromImage($crop)
        $g.SmoothingMode      = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g.InterpolationMode  = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.PixelOffsetMode    = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $g.DrawImage(
            $src,
            (New-Object System.Drawing.Rectangle 0, 0, $side, $side),
            (New-Object System.Drawing.Rectangle $offX, $offY, $side, $side),
            [System.Drawing.GraphicsUnit]::Pixel)
        $g.Dispose()

        $final = New-Object System.Drawing.Bitmap 1024, 1024, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g2    = [System.Drawing.Graphics]::FromImage($final)
        $g2.SmoothingMode      = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g2.InterpolationMode  = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g2.PixelOffsetMode    = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g2.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $g2.DrawImage($crop, 0, 0, 1024, 1024)
        $g2.Dispose()

        $final.Save($IconPng, [System.Drawing.Imaging.ImageFormat]::Png)
        $final.Dispose()
        $crop.Dispose()
    }
    finally {
        $src.Dispose()
    }
}

# ── Render a single icon size by resampling the canonical PNG ────────────────
function Render-Size([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode      = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode  = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode    = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    $src = [System.Drawing.Image]::FromFile($IconPng)
    try {
        $g.DrawImage($src, 0, 0, $size, $size)
    }
    finally {
        $src.Dispose()
        $g.Dispose()
    }
    return $bmp
}

# ── Write multi-resolution ICO (PNG-in-ICO, Vista+) ──────────────────────────
function Write-IcoFile([System.Drawing.Bitmap[]]$maps, [string]$path) {
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)

    $bw.Write([uint16]0)
    $bw.Write([uint16]1)
    $bw.Write([uint16]$maps.Count)

    $pngList = @()
    foreach ($bmp in $maps) {
        $pms = New-Object System.IO.MemoryStream
        $bmp.Save($pms, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngList += , ($pms.ToArray())
        $pms.Dispose()
    }

    [int]$offset = 6 + 16 * $maps.Count
    for ($i = 0; $i -lt $maps.Count; $i++) {
        $w = $maps[$i].Width;  $h = $maps[$i].Height
        $bw.Write([byte]$(if ($w -eq 256) { 0 } else { [byte]$w }))
        $bw.Write([byte]$(if ($h -eq 256) { 0 } else { [byte]$h }))
        $bw.Write([byte]0)
        $bw.Write([byte]0)
        $bw.Write([uint16]1)
        $bw.Write([uint16]32)
        $bw.Write([uint32]$pngList[$i].Length)
        $bw.Write([uint32]$offset)
        $offset += $pngList[$i].Length
    }

    foreach ($data in $pngList) { $bw.Write($data) }

    $bw.Flush()
    [System.IO.File]::WriteAllBytes($path, $ms.ToArray())
    $ms.Dispose(); $bw.Dispose()
}

Ensure-SquareCanonical

$sizes   = @(256, 128, 64, 48, 32, 16)
$bitmaps = @()
foreach ($sz in $sizes) {
    $bitmaps += (Render-Size $sz)
    Write-Host ("  Rendered {0}x{0}" -f $sz)
}

Write-Host "`nWriting ICO..."
Write-IcoFile $bitmaps $OutputIco
foreach ($bmp in $bitmaps) { $bmp.Dispose() }

Write-Host "Done: $OutputIco"
