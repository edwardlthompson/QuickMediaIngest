#nullable enable
using System;
using System.IO;
using System.Text;

namespace QuickMediaIngest.ViewModels
{
    internal static class UpdateHandoffScriptBuilder
    {
        public static string Build(string downloadedUpdatePath, string ext, string currentExePath, int currentPid, string packageType)
        {
            string tempScript = Path.Combine(
                Path.GetTempPath(),
                "QuickMediaIngest",
                "updates",
                $"apply-update-{DateTime.UtcNow:yyyyMMddHHmmssfff}.cmd");
            Directory.CreateDirectory(Path.GetDirectoryName(tempScript) ?? Path.GetTempPath());

            string script = $@"@echo off
setlocal enableextensions
set ""QMI_UPDATE_FILE={downloadedUpdatePath}""
set ""QMI_CURRENT_EXE={currentExePath}""
set ""QMI_PID={currentPid}""
set ""QMI_PACKAGE={packageType}""
set ""QMI_EXT={ext}""

for /L %%i in (1,1,180) do (
  tasklist /FI ""PID eq %QMI_PID%"" | findstr /I /C:""%QMI_PID%"" >nul
  if errorlevel 1 goto :ready
  timeout /t 1 /nobreak >nul
)

:ready
if /I ""%QMI_EXT%""=="".msi"" (
  start """" /wait msiexec /i ""%QMI_UPDATE_FILE%"" /passive /norestart
  start """" ""%QMI_CURRENT_EXE%""
  goto :cleanup
)

if /I ""%QMI_EXT%""=="".exe"" (
  if /I ""%QMI_PACKAGE%""==""Portable"" (
    copy /Y ""%QMI_UPDATE_FILE%"" ""%QMI_CURRENT_EXE%"" >nul
    start """" ""%QMI_CURRENT_EXE%""
  ) else (
    start """" ""%QMI_UPDATE_FILE%""
  )
  goto :cleanup
)

:cleanup
del /Q ""%QMI_UPDATE_FILE%"" >nul 2>nul
del /Q ""%~f0"" >nul 2>nul
";

            File.WriteAllText(tempScript, script, Encoding.ASCII);
            return tempScript;
        }
    }
}
