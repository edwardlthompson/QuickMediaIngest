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
        private static async Task<bool> PullFileAsync(
            string serial,
            string devicePath,
            string localPath,
            CancellationToken cancellationToken)
        {
            TryDelete(localPath);
            return await RunAdbAsync(serial, $"pull \"{devicePath}\" \"{localPath}\"", cancellationToken)
                .ConfigureAwait(false);
        }

        private static bool LooksLikeJpeg(string path)
        {
            try
            {
                if (!File.Exists(path) || new FileInfo(path).Length < 64)
                {
                    return false;
                }

                Span<byte> header = stackalloc byte[2];
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                return fs.Read(header) >= 2 && header[0] == 0xFF && header[1] == 0xD8;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<string> RunAdbShellAsync(
            string serial,
            string shellCommand,
            CancellationToken cancellationToken)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "adb",
                Arguments = $"-s {serial} shell {shellCommand}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return string.Empty;
            }

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(WallTimeout);
            try
            {
                string stdout = await process.StandardOutput.ReadToEndAsync(linked.Token).ConfigureAwait(false);
                await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
                return stdout;
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                cancellationToken.ThrowIfCancellationRequested();
                return string.Empty;
            }
        }

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
                await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
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
