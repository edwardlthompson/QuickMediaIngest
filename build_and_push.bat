@echo off
echo ==========================================
echo 🔍 1. Calculating Next Version...
echo ==========================================

:: Run helper script to calculate versions
for /f "tokens=1,2,3,4,5,6,7 delims=|" %%a in ('powershell -ExecutionPolicy Bypass -File calculate_version.ps1') do (
    set CURRENT_VERSION=%%a
    set PATCH_VERSION=%%b
    set MINOR_VERSION=%%c
    set MAJOR_VERSION=%%d
    set RECOMMENDED_VERSION=%%e
    set RECOMMENDED_BUMP=%%f
    set BUMP_REASON=%%g
)

echo Current Version: %CURRENT_VERSION%
echo Recommended Bump: %RECOMMENDED_BUMP% ^(%RECOMMENDED_VERSION%^) 
echo Reason: %BUMP_REASON%
echo.

:: Optional override: set VERSION_BUMP=patch^|minor^|major before running this script.
if /I "%VERSION_BUMP%"=="major" set NEW_VERSION=%MAJOR_VERSION%
if /I "%VERSION_BUMP%"=="minor" set NEW_VERSION=%MINOR_VERSION%
if /I "%VERSION_BUMP%"=="patch" set NEW_VERSION=%PATCH_VERSION%

if not defined NEW_VERSION set NEW_VERSION=%RECOMMENDED_VERSION%

:SET_VERSION
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
call build_local_test.bat %NEW_VERSION%

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Build failed. Verify .NET 8 SDK is installed.
    timeout /t 5
    exit /b
)

echo.
echo [SUCCESS] Local EXE built at: publish\local-test\QuickMediaIngest.exe
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
    timeout /t 5
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

timeout /t 3
