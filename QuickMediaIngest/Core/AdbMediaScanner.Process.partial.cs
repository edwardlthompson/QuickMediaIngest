#nullable enable
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace QuickMediaIngest.Core
{
    public sealed partial class AdbMediaScanner
    {
        private static async Task<string?> RunAdbCaptureAsync(
            string serial,
            string argsWithoutSerial,
            CancellationToken cancellationToken)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "adb",
                Arguments = $"-s {serial} {argsWithoutSerial}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return null;
            }

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(WallTimeout);
            try
            {
                // Drain both pipes concurrently so large stderr progress cannot deadlock WaitForExit.
                Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(linked.Token);
                Task<string> stderrTask = process.StandardError.ReadToEndAsync(linked.Token);
                await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
                await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
                string stdout = await stdoutTask.ConfigureAwait(false);
                return process.ExitCode == 0 ? stdout : null;
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                cancellationToken.ThrowIfCancellationRequested();
                return null;
            }
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
                // ignore
            }
        }
    }
}
