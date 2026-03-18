@echo off
echo ==========================================
echo 🔍 1. Calculating Next Version...
echo ==========================================

:: Run helper script to calculate versions
for /f "tokens=1,2,3,4 delims=|" %%a in ('powershell -File calculate_version.ps1') do (
    set CURRENT_VERSION=%%a
    set PATCH_VERSION=%%b
    set MINOR_VERSION=%%c
    set MAJOR_VERSION=%%d
)

echo Current Version: %CURRENT_VERSION%
echo Proposing options (Semantic Versioning):
echo   [1] Patch Bump -> %PATCH_VERSION% (Default)
echo   [2] Minor Bump -> %MINOR_VERSION%
echo   [3] Major Bump -> %MAJOR_VERSION%
echo.

set /p CHOICE="Enter choice [1-3, or type custom version, Press Enter for 1]: "

if "%CHOICE%"=="1" set NEW_VERSION=%PATCH_VERSION%
if "%CHOICE%"=="2" set NEW_VERSION=%MINOR_VERSION%
if "%CHOICE%"=="3" set NEW_VERSION=%MAJOR_VERSION%
if "%CHOICE%"=="" set NEW_VERSION=%PATCH_VERSION%

:: If they typed a custom version (not 1,2,3 and not empty)
if not "%CHOICE%"=="1" if not "%CHOICE%"=="2" if not "%CHOICE%"=="3" if not "%CHOICE%"=="" set NEW_VERSION=%CHOICE%

echo Setting version to %NEW_VERSION%...

set CSPROJ=QuickMediaIngest\QuickMediaIngest.csproj

:: Update .csproj
powershell -Command ^
  "$content = Get-Content %CSPROJ% -Raw;" ^
  "$newContent = $content -replace '<Version>.*?</Version>', ('<Version>%NEW_VERSION%</Version>');" ^
  "Set-Content %CSPROJ% -Value $newContent -NoNewline;"

echo.
echo ==========================================
echo 🛠️ 2. Building Portable EXE Locally...
echo ==========================================
dotnet publish QuickMediaIngest\QuickMediaIngest.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:Version=%NEW_VERSION% -o ./publish/portable

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Build failed. Verify .NET 8 SDK is installed.
    pause
    exit /b
)

echo.
echo [SUCCESS] Local EXE built at: publish\portable\QuickMediaIngest.exe
echo.

echo ==========================================
echo 📂 3. Committing Changes...
echo ==========================================
git add .
git commit -m "Bump version to %NEW_VERSION%"

echo.
echo ==========================================
echo 🚀 4. Pushing to GitHub & Creating Release...
echo ==========================================
git push origin main

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Failed to push to main.
    pause
    exit /b
)

echo Adding tag v%NEW_VERSION%...
git tag v%NEW_VERSION%
git push origin v%NEW_VERSION%

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Failed to push tag.
) else (
    echo.
    echo [SUCCESS] Pushed to GitHub and triggered Release Build!
    echo Check the 'Actions' or 'Releases' tab on GitHub.
)

pause
