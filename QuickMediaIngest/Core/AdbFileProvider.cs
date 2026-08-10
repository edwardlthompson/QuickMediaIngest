#nullable enable
using System;
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

        public Task CopyAsync(
            string srcPath,
            string destPath,
            CancellationToken token,
            IProgress<long>? bytesCopied = null,
            long expectedBytes = 0) =>
            RunAdbAsync(
                $"pull \"{srcPath}\" \"{destPath}\"",
                $"ADB pull: {srcPath} -> {destPath}",
                "ADB pull failed",
                token,
                AdbPullTimeout.Compute(expectedBytes),
                afterSuccess: () =>
                {
                    if (bytesCopied != null && File.Exists(destPath))
                    {
                        bytesCopied.Report(new FileInfo(destPath).Length);
                    }
                });

        public Task DeleteAsync(string srcPath, CancellationToken token) =>
            RunAdbAsync(
                $"shell rm \"{srcPath}\"",
                $"ADB delete: {srcPath}",
                "ADB delete failed",
                token,
                AdbPullTimeout.Floor);

        private async Task RunAdbAsync(
            string adbArgumentsWithoutSerial,
            string startLog,
            string failurePrefix,
            CancellationToken token,
            TimeSpan wallTimeout,
            Action? afterSuccess = null)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "adb",
                Arguments = $"-s {_deviceSerial} {adbArgumentsWithoutSerial}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            _logger.LogInformation("{StartLog}", startLog);
            using var process = Process.Start(psi);
            if (process == null)
            {
                throw new IOException("Failed to start adb process.");
            }

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token);
            linked.CancelAfter(wallTimeout);
            try
            {
                await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                token.ThrowIfCancellationRequested();
                throw new TimeoutException(
                    $"{failurePrefix}: timed out after {wallTimeout.TotalMinutes:0.##} minutes.");
            }

            if (process.ExitCode != 0)
            {
                string error = await process.StandardError.ReadToEndAsync(token).ConfigureAwait(false);
                throw new IOException($"{failurePrefix}: {error}");
            }

            afterSuccess?.Invoke();
        }

        private static void TryKill(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Best-effort kill on cancel/timeout.
            }
        }
    }
}
