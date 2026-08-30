#nullable enable
using System;
using System.Diagnostics;
using System.IO;

namespace QuickMediaIngest.Core.Services
{
    public enum VolumeEncryptionStatus
    {
        Unknown = 0,
        NotEncrypted = 1,
        BitLockerEncrypted = 2,
        VeraCryptDetected = 3,
    }

    /// <summary>
    /// Passive detection of volume encryption status (BitLocker via manage-bde or VeraCrypt mount points).
    /// Read-only check with strict timeout; never executes administrative changes or blocks UI.
    /// </summary>
    public static class DestinationEncryptionDetector
    {
        public static VolumeEncryptionStatus DetectEncryption(string? folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return VolumeEncryptionStatus.Unknown;
            }

            try
            {
                string? root = Path.GetPathRoot(Path.GetFullPath(folderPath));
                if (string.IsNullOrWhiteSpace(root))
                {
                    return VolumeEncryptionStatus.Unknown;
                }

                string driveLetter = root.TrimEnd('\\');

                // 1. BitLocker check on Windows via manage-bde -status
                if (OperatingSystem.IsWindows() && driveLetter.Length == 2 && driveLetter[1] == ':')
                {
                    var bitlockerStatus = CheckBitLockerStatus(driveLetter);
                    if (bitlockerStatus != VolumeEncryptionStatus.Unknown)
                    {
                        return bitlockerStatus;
                    }
                }

                // 2. VeraCrypt / TrueCrypt heuristic (volume format label / process or VeraCrypt device)
                if (OperatingSystem.IsWindows() && CheckVeraCryptHeuristic(root))
                {
                    return VolumeEncryptionStatus.VeraCryptDetected;
                }
            }
            catch
            {
                // Ignore detection failures; fail-soft to Unknown
            }

            return VolumeEncryptionStatus.Unknown;
        }

        private static VolumeEncryptionStatus CheckBitLockerStatus(string driveLetter)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "manage-bde.exe",
                    Arguments = $"-status {driveLetter}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using var proc = Process.Start(psi);
                if (proc == null)
                {
                    return VolumeEncryptionStatus.Unknown;
                }

                if (!proc.WaitForExit(1000))
                {
                    proc.Kill();
                    return VolumeEncryptionStatus.Unknown;
                }

                string output = proc.StandardOutput.ReadToEnd();
                if (output.Contains("Protection On", StringComparison.OrdinalIgnoreCase) ||
                    output.Contains("Fully Encrypted", StringComparison.OrdinalIgnoreCase) ||
                    output.Contains("100.0%", StringComparison.OrdinalIgnoreCase) && output.Contains("Conversion Status", StringComparison.OrdinalIgnoreCase))
                {
                    return VolumeEncryptionStatus.BitLockerEncrypted;
                }

                if (output.Contains("Protection Off", StringComparison.OrdinalIgnoreCase) ||
                    output.Contains("Fully Decrypted", StringComparison.OrdinalIgnoreCase))
                {
                    return VolumeEncryptionStatus.NotEncrypted;
                }
            }
            catch
            {
                // manage-bde might not be installed (Windows Home edition) or permissions denied
            }

            return VolumeEncryptionStatus.Unknown;
        }

        private static bool CheckVeraCryptHeuristic(string root)
        {
            try
            {
                var driveInfo = new DriveInfo(root);
                if (driveInfo.DriveFormat.Equals("VeraCrypt", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch
            {
                // Ignore drive info exceptions
            }

            return false;
        }
    }
}
