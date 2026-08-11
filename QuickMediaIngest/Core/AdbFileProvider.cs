#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        public Task DeleteAsync(string srcPath, CancellationToken token)
        {
            // Android sh treats & as a command separator — double quotes do NOT protect it.
            // Prefer single-quoted path (escape embedded single quotes for POSIX sh).
            string quoted = "'" + srcPath.Replace("'", "'\\''", StringComparison.Ordinal) + "'";
            return RunAdbAsync(
                $"shell rm {quoted}",
                $"ADB delete: {srcPath}",
                "ADB delete failed",
                token,
                AdbPullTimeout.Floor);
        }

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

            // Drain stdout/stderr while waiting — otherwise adb progress fills the pipe and deadlocks.
            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
            Task<string> stderrTask = process.StandardError.ReadToEndAsync();

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token);
            linked.CancelAfter(wallTimeout);
            try
            {
                await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
                await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
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
                string stderr = await stderrTask.ConfigureAwait(false);
                string stdout = await stdoutTask.ConfigureAwait(false);
                string detail = string.Join(" | ", new[] { stderr.Trim(), stdout.Trim() }.Where(s => s.Length > 0));
                if (IsBenignMissingDelete(failurePrefix, detail))
                {
                    _logger.LogInformation("ADB delete skipped; remote file already absent.");
                    return;
                }

                throw new IOException(
                    string.IsNullOrEmpty(detail) ? $"{failurePrefix} (exit {process.ExitCode})." : $"{failurePrefix}: {detail}");
            }

            afterSuccess?.Invoke();
        }

        internal static bool IsBenignMissingDelete(string failurePrefix, string detail) =>
            failurePrefix.StartsWith("ADB delete", StringComparison.Ordinal)
            && (detail.Contains("No such file", StringComparison.OrdinalIgnoreCase)
                || detail.Contains("does not exist", StringComparison.OrdinalIgnoreCase));

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
