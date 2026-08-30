#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core.Services
{
    public sealed class TranscodeOptions
    {
        public bool Enabled { get; set; } = false;
        public string TargetCodec { get; set; } = "libx264";
        public string TargetContainer { get; set; } = ".mp4";
        public string Preset { get; set; } = "fast";
        public int Crf { get; set; } = 22;
    }

    public static class FfmpegTranscoder
    {
        public static string BuildArguments(string inputPath, string outputPath, TranscodeOptions options)
        {
            return $"-y -i \"{inputPath}\" -c:v {options.TargetCodec} -crf {options.Crf} -preset {options.Preset} -c:a aac \"{outputPath}\"";
        }

        public static async Task<bool> TryTranscodeAsync(string inputPath, string outputPath, TranscodeOptions options, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            if (!options.Enabled || !File.Exists(inputPath))
            {
                return false;
            }

            try
            {
                string args = BuildArguments(inputPath, outputPath, options);
                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(psi);
                if (process == null) return false;
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "FFmpeg transcode failed or ffmpeg not in PATH for {InputPath}", inputPath);
                return false;
            }
        }
    }
}
