#nullable enable
using System;

namespace QuickMediaIngest.Core
{
    /// <summary>Result of ADB transfer preflight for an FTP remote folder.</summary>
    public readonly record struct AdbTransferSession(string DeviceSerial, string MediaRootPrefix);

    /// <summary>Resolves whether hybrid FTP→ADB transfer can be used.</summary>
    public static class AdbTransferEligibility
    {
        public static AdbTransferSession? TryResolve(string ftpRemoteFolder, IAdbPathProbe? pathProbe = null)
        {
            string? serial = AdbDeviceProbe.GetFirstDeviceSerial();
            if (string.IsNullOrWhiteSpace(serial))
            {
                return null;
            }

            IAdbPathProbe probe = pathProbe ?? new AdbShellPathProbe();
            foreach (string candidate in AdbAndroidPath.CandidateDirectoryRoots(ftpRemoteFolder))
            {
                if (!probe.DirectoryExists(serial, candidate))
                {
                    continue;
                }

                if (AdbAndroidPath.TryGetMediaRootPrefix(candidate, ftpRemoteFolder, out string prefix))
                {
                    return new AdbTransferSession(serial, prefix);
                }
            }

            return null;
        }

        public static string FormatSerialSuffix(string deviceSerial)
        {
            if (string.IsNullOrEmpty(deviceSerial))
            {
                return string.Empty;
            }

            return deviceSerial.Length <= 4
                ? deviceSerial
                : deviceSerial[^Math.Min(4, deviceSerial.Length)..];
        }
    }
}
