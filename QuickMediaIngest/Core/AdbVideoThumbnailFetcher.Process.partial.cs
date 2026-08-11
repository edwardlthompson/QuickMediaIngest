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
                Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(linked.Token);
                Task stderrTask = process.StandardError.ReadToEndAsync(linked.Token);
                await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
                await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
                return await stdoutTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                cancellationToken.ThrowIfCancellationRequested();
                return string.Empty;
            }
        }
    }
}
