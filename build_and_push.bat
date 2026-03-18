@echo off
set /p COMMIT_MSG="Enter Commit Message (Default: 'Auto Build & Sync'): "
if "%COMMIT_MSG%"=="" set COMMIT_MSG=Auto Build & Sync

echo.
echo ==========================================
echo 🛠️ 1. Building Portable EXE Locally...
echo ==========================================
dotnet publish QuickMediaIngest\QuickMediaIngest.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o ./publish/portable

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
echo 📂 2. Committing Changes...
echo ==========================================
git add .
git commit -m "%COMMIT_MSG%"

echo.
echo ==========================================
echo 🚀 3. Pushing to GitHub...
echo ==========================================
git push origin main

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Push failed. Verify remote URL or credentials.
) else (
    echo.
    echo [SUCCESS] Synced with GitHub successfully!
)

pause
