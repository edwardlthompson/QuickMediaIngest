#nullable enable
using System;
using System.Diagnostics;

namespace QuickMediaIngest.Core
{
    /// <summary>Uses <c>adb shell</c> to test remote directories and files.</summary>
    public sealed class AdbShellPathProbe : IAdbPathProbe
    {
        public bool DirectoryExists(string deviceSerial, string remoteDirectory) =>
            ProbeExists(deviceSerial, remoteDirectory, directory: true);

        public bool FileExists(string deviceSerial, string remoteFilePath) =>
            ProbeExists(deviceSerial, remoteFilePath, directory: false);

        private static bool ProbeExists(string deviceSerial, string remotePath, bool directory)
        {
            if (string.IsNullOrWhiteSpace(deviceSerial) || string.IsNullOrWhiteSpace(remotePath))
            {
                return false;
            }

            if (!AdbAndroidPath.TryNormalizeRemote(remotePath, out string path))
            {
                return false;
            }

            try
            {
                string escaped = path.Replace("'", "'\\''", StringComparison.Ordinal);
                string test = directory ? $"[ -d '{escaped}' ] && echo OK" : $"[ -f '{escaped}' ] && echo OK";
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = $"-s {deviceSerial} shell \"{test}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });
                if (process == null || !process.WaitForExit(8000))
                {
                    try
                    {
                        process?.Kill(entireProcessTree: true);
                    }
                    catch
                    {
                        // ignore
                    }

                    return false;
                }

                if (process.ExitCode != 0)
                {
                    return false;
                }

                string output = process.StandardOutput.ReadToEnd();
                return output.Contains("OK", StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }
    }
}
