# create_icon.ps1
# Generates QuickMediaIngest.ico from the icon design using .NET GDI+.
# Run: powershell -ExecutionPolicy Bypass -File create_icon.ps1
#
# Design: dark background  |  white SD-card  |  electric-cyan down-arrow
# Produces a multi-resolution ICO: 256, 128, 64, 48, 32, 16 px

Add-Type -AssemblyName System.Drawing

$OutputIco = Join-Path $PSScriptRoot "QuickMediaIngest\QuickMediaIngest.ico"

$colorBg    = [System.Drawing.ColorTranslator]::FromHtml("#0d1117")
$colorCard  = [System.Drawing.ColorTranslator]::FromHtml("#e2e8f0")
$colorArrow = [System.Drawing.ColorTranslator]::FromHtml("#00d4ff")

# ── Helper: build a GraphicsPath for a rounded rectangle ──────────────────────
function New-RoundedRectPath([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2.0
    $path.AddArc($x,           $y,           $d, $d, 180, 90)
    $path.AddArc($x + $w - $d, $y,           $d, $d, 270, 90)
    $path.AddArc($x + $w - $d, $y + $h - $d, $d, $d,   0, 90)
    $path.AddArc($x,           $y + $h - $d, $d, $d,  90, 90)
    $path.CloseFigure()
    return $path
}

# ── Draw one size of the icon ─────────────────────────────────────────────────
function Draw-Icon {
    param([System.Drawing.Graphics]$g, [int]$size)

    $g.SmoothingMode       = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode     = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.CompositingQuality  = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $g.InterpolationMode   = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

    # Scale factor against the 256-unit design grid
    [float]$s = $size / 256.0

    # ── Background ────────────────────────────────────────────────────────────
    $bgPath  = New-RoundedRectPath 0 0 $size $size ([float]([Math]::Max(2, 40 * $s)))
    $bgBrush = New-Object System.Drawing.SolidBrush($colorBg)
    $g.FillPath($bgBrush, $bgPath)
    $bgBrush.Dispose(); $bgPath.Dispose()

    # ── SD card (draw for sizes ≥ 32) ─────────────────────────────────────────
    if ($size -ge 32) {
        $cardBrush = New-Object System.Drawing.SolidBrush($colorCard)
        $cardPts   = @(
            [System.Drawing.PointF]::new([float](73  * $s), [float](68  * $s)),
            [System.Drawing.PointF]::new([float](150 * $s), [float](68  * $s)),
            [System.Drawing.PointF]::new([float](183 * $s), [float](98  * $s)),
            [System.Drawing.PointF]::new([float](183 * $s), [float](213 * $s)),
            [System.Drawing.PointF]::new([float](73  * $s), [float](213 * $s))
        )
        $g.FillPolygon($cardBrush, $cardPts)
        $cardBrush.Dispose()

        # Connector slots — only at sizes where they are clearly visible
        if ($size -ge 64) {
            $slotBrush = New-Object System.Drawing.SolidBrush($colorBg)
            foreach ($xi in @(85, 109, 133, 157)) {
                $g.FillRectangle(
                    $slotBrush,
                    [float]($xi * $s),
                    [float](192 * $s),
                    [float](14  * $s),
                    [float](21  * $s)
                )
            }
            $slotBrush.Dispose()
        }
    }

    # ── Down arrow ────────────────────────────────────────────────────────────
    $arrowBrush = New-Object System.Drawing.SolidBrush($colorArrow)

    if ($size -ge 32) {
        # Full-detail downward arrow (shaft + wide chevron head)
        #  shaft  : x 108–148,  y 20–115
        #  head   : x 86–170,   tip (128, 163)
        $ap = @(
            [System.Drawing.PointF]::new([float](108 * $s), [float]( 20 * $s)),
            [System.Drawing.PointF]::new([float](148 * $s), [float]( 20 * $s)),
            [System.Drawing.PointF]::new([float](148 * $s), [float](115 * $s)),
            [System.Drawing.PointF]::new([float](170 * $s), [float](115 * $s)),
            [System.Drawing.PointF]::new([float](128 * $s), [float](163 * $s)),
            [System.Drawing.PointF]::new([float]( 86 * $s), [float](115 * $s)),
            [System.Drawing.PointF]::new([float](108 * $s), [float](115 * $s))
        )
    }
    else {
        # Simplified bold arrow for 16px — fills almost the whole icon
        [float]$m = $size / 16.0
        $ap = @(
            [System.Drawing.PointF]::new([float](5  * $m), [float]( 2 * $m)),
            [System.Drawing.PointF]::new([float](11 * $m), [float]( 2 * $m)),
            [System.Drawing.PointF]::new([float](11 * $m), [float]( 9 * $m)),
            [System.Drawing.PointF]::new([float](14 * $m), [float]( 9 * $m)),
            [System.Drawing.PointF]::new([float]( 8 * $m), [float](14 * $m)),
            [System.Drawing.PointF]::new([float]( 2 * $m), [float]( 9 * $m)),
            [System.Drawing.PointF]::new([float]( 5 * $m), [float]( 9 * $m))
        )
    }

    $g.FillPolygon($arrowBrush, $ap)
    $arrowBrush.Dispose()
}

# ── Render each standard icon size ───────────────────────────────────────────
$sizes   = @(256, 128, 64, 48, 32, 16)
$bitmaps = @()

foreach ($sz in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap(
        $sz, $sz, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $gr  = [System.Drawing.Graphics]::FromImage($bmp)
    Draw-Icon $gr $sz
    $gr.Dispose()
    $bitmaps += $bmp
    Write-Host "  Rendered ${sz}×${sz}"
}

# ── Write multi-resolution ICO (PNG-in-ICO format, Vista+) ───────────────────
function Write-IcoFile([System.Drawing.Bitmap[]]$maps, [string]$path) {
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)

    # ICONDIR header
    $bw.Write([uint16]0)              # reserved
    $bw.Write([uint16]1)              # type: 1 = icon
    $bw.Write([uint16]$maps.Count)    # number of images

    # Collect PNG bytes for each bitmap
    $pngList = @()
    foreach ($bmp in $maps) {
        $pms = New-Object System.IO.MemoryStream
        $bmp.Save($pms, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngList += (, $pms.ToArray())
        $pms.Dispose()
    }

    # ICONDIRENTRY records (16 bytes each)
    [int]$offset = 6 + 16 * $maps.Count
    for ($i = 0; $i -lt $maps.Count; $i++) {
        $w = $maps[$i].Width;  $h = $maps[$i].Height
        $bw.Write([byte]$(if ($w -eq 256) { 0 } else { [byte]$w }))
        $bw.Write([byte]$(if ($h -eq 256) { 0 } else { [byte]$h }))
        $bw.Write([byte]0)      # color count (0 = truecolor)
        $bw.Write([byte]0)      # reserved
        $bw.Write([uint16]1)    # color planes
        $bw.Write([uint16]32)   # bits per pixel
        $bw.Write([uint32]$pngList[$i].Length)
        $bw.Write([uint32]$offset)
        $offset += $pngList[$i].Length
    }

    # Image data
    foreach ($data in $pngList) { $bw.Write($data) }

    $bw.Flush()
    [System.IO.File]::WriteAllBytes($path, $ms.ToArray())
    $ms.Dispose(); $bw.Dispose()
}

Write-Host "`nWriting ICO..."
Write-IcoFile $bitmaps $OutputIco

foreach ($bmp in $bitmaps) { $bmp.Dispose() }

Write-Host "Done: $OutputIco"
