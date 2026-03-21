# Screenshot Automation for QuickMediaIngest

This PowerShell script launches the app, waits for you to arrange the desired UI, and then opens the screenshots folder for you to save captures. Use Windows+Shift+S or Snipping Tool to take screenshots and save them in the opened folder.

You can run this script multiple times for each scenario (main window, FTP, ingest progress, unified view, onboarding, etc.).

---

```powershell
# launch_and_prepare_screenshots.ps1
$exePath = "..\..\publish\local-test\QuickMediaIngest.exe"
$screenshotDir = "$(Split-Path -Parent $MyInvocation.MyCommand.Path)\screenshots"

Start-Process $exePath
Start-Sleep -Seconds 5  # Wait for app to launch
Invoke-Item $screenshotDir
Write-Host "Arrange the app window and use Windows+Shift+S to capture screenshots. Save them in the screenshots folder."
```

---

## Usage
1. Run this script from the Assets directory:
   ```
   pwsh .\launch_and_prepare_screenshots.ps1
   ```
2. Arrange the app for each scenario and save screenshots as PNGs in the screenshots folder.
3. Rename files descriptively (e.g., main-window.png, ftp-view.png, ingest-progress.png, unified-view.png, onboarding.png).
4. Add the screenshots to the README.md as shown in the template below.
