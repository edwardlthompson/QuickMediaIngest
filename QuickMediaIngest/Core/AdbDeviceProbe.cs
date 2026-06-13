#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace QuickMediaIngest.Core
{
    /// <summary>Detects connected Android devices via the adb CLI.</summary>
    public static class AdbDeviceProbe
    {
        public static bool IsAdbAvailable()
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = "version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });
                return process != null && process.WaitForExit(5000) && process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public static IReadOnlyList<string> ListDeviceSerials()
        {
            if (!IsAdbAvailable())
            {
                return Array.Empty<string>();
            }

            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = "devices",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });
                if (process == null || !process.WaitForExit(10000) || process.ExitCode != 0)
                {
                    return Array.Empty<string>();
                }

                string output = process.StandardOutput.ReadToEnd();
                return output
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Skip(1)
                    .Select(line => line.Trim())
                    .Where(line => line.EndsWith("\tdevice", StringComparison.Ordinal) || line.EndsWith(" device", StringComparison.Ordinal))
                    .Select(line => line.Split('\t', ' ')[0])
                    .Where(serial => !string.IsNullOrWhiteSpace(serial))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public static string? GetFirstDeviceSerial() => ListDeviceSerials().FirstOrDefault();
    }
}
