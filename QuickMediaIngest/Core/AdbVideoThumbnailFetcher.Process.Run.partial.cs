#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace QuickMediaIngest.Core
{
    public sealed partial class AdbVideoThumbnailFetcher
    {
        private static async Task<bool> RunAdbAsync(string serial, string args, CancellationToken cancellationToken)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "adb",
                Arguments = $"-s {serial} {args}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return false;
            }

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(WallTimeout);
            try
            {
                Task stdoutTask = process.StandardOutput.ReadToEndAsync(linked.Token);
                Task stderrTask = process.StandardError.ReadToEndAsync(linked.Token);
                await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
                await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
                return process.ExitCode == 0;
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                cancellationToken.ThrowIfCancellationRequested();
                return false;
            }
        }

        private static async Task<bool> RunAdbExecOutToFileAsync(
            string serial,
            string shellCommand,
            string localPath,
            long maxBytes,
            CancellationToken cancellationToken)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "adb",
                Arguments = $"-s {serial} exec-out {shellCommand}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return false;
            }

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(WallTimeout);
            try
            {
                // Drain stderr while copying exec-out bytes from stdout (progress/noise must not fill the pipe).
                Task stderrTask = process.StandardError.ReadToEndAsync(linked.Token);
                await using var dest = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None);
                byte[] buffer = new byte[65536];
                long total = 0;
                var stdout = process.StandardOutput.BaseStream;
                while (total < maxBytes)
                {
                    int toRead = (int)Math.Min(buffer.Length, maxBytes - total);
                    int read = await stdout.ReadAsync(buffer.AsMemory(0, toRead), linked.Token).ConfigureAwait(false);
                    if (read <= 0)
                    {
                        break;
                    }

                    await dest.WriteAsync(buffer.AsMemory(0, read), linked.Token).ConfigureAwait(false);
                    total += read;
                }

                await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
                await stderrTask.ConfigureAwait(false);
                return total > 0 && process.ExitCode == 0;
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                TryDelete(localPath);
                cancellationToken.ThrowIfCancellationRequested();
                return false;
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

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
