#nullable enable
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Provides file operations for Android devices using ADB (Android Debug Bridge).
    /// </summary>
    public class AdbFileProvider : IFileProvider
    {
        private readonly string _deviceSerial;
        private readonly ILogger<AdbFileProvider> _logger;

        public AdbFileProvider(string deviceSerial, ILogger<AdbFileProvider> logger)
        {
            _deviceSerial = deviceSerial;
            _logger = logger;
        }

        public async Task CopyAsync(string srcPath, string destPath, CancellationToken token)
        {
            // Use adb pull to copy file from device to local
            var psi = new ProcessStartInfo
            {
                FileName = "adb",
                Arguments = $"-s {_deviceSerial} pull \"{srcPath}\" \"{destPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            _logger.LogInformation("ADB pull: {SrcPath} -> {DestPath}", srcPath, destPath);
            using var process = Process.Start(psi);
            if (process == null)
                throw new IOException("Failed to start adb process.");
            await process.WaitForExitAsync(token);
            if (process.ExitCode != 0)
            {
                string error = await process.StandardError.ReadToEndAsync();
                throw new IOException($"ADB pull failed: {error}");
            }
        }

        public async Task DeleteAsync(string srcPath, CancellationToken token)
        {
            // Use adb shell rm to delete file on device
            var psi = new ProcessStartInfo
            {
                FileName = "adb",
                Arguments = $"-s {_deviceSerial} shell rm \"{srcPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            _logger.LogInformation("ADB delete: {SrcPath}", srcPath);
            using var process = Process.Start(psi);
            if (process == null)
                throw new IOException("Failed to start adb process.");
            await process.WaitForExitAsync(token);
            if (process.ExitCode != 0)
            {
                string error = await process.StandardError.ReadToEndAsync();
                throw new IOException($"ADB delete failed: {error}");
            }
        }
    }
}
