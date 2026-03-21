@echo off
setlocal

set VERSION=%~1

if "%VERSION%"=="" (
    for /f "tokens=*" %%v in ('powershell -NoProfile -Command "$csproj='QuickMediaIngest/QuickMediaIngest.csproj'; if (Test-Path $csproj) { $content = Get-Content $csproj -Raw; $m = [regex]::Match($content, '<Version>([^<]+)</Version>'); if ($m.Success) { $m.Groups[1].Value.Trim() } else { '1.0.0' } } else { '1.0.0' }"') do set VERSION=%%v
)

echo ==========================================
echo Building local portable test app (v%VERSION%)...
echo Output: publish\local-test\QuickMediaIngest.exe
echo ==========================================

dotnet publish QuickMediaIngest\QuickMediaIngest.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishReadyToRun=true /p:Version=%VERSION% -o ./publish/local-test

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Local test build failed. Verify .NET 8 SDK is installed.
    exit /b 1
)

echo.
echo [SUCCESS] Local test build is ready: publish\local-test\QuickMediaIngest.exe
exit /b 0
