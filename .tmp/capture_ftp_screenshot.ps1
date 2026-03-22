$env:QMI_SHOW_FTP_ON_LAUNCH='1'
$exe = Join-Path (Resolve-Path 'QuickMediaIngest/bin/Debug/net8.0-windows').Path 'QuickMediaIngest.exe'
if (-not (Test-Path $exe)) { Write-Error "Executable not found: $exe"; exit 1 }
$proc = Start-Process -FilePath $exe -PassThru
Start-Sleep -Seconds 2
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$bmp = New-Object System.Drawing.Bitmap($bounds.Width, $bounds.Height)
$graphics = [System.Drawing.Graphics]::FromImage($bmp)
$graphics.CopyFromScreen([System.Drawing.Point]::Empty, [System.Drawing.Point]::Empty, $bmp.Size)
$out = 'QuickMediaIngest/Assets/screenshots/ftp-overlay-debug.png'
$dir = Split-Path $out
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose()
$bmp.Dispose()
Write-Output "Saved screenshot to $out"
