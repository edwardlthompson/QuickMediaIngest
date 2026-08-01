#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core
{
    /// <summary>Optional ffmpeg CLI frame extract for complete local video files (Shell backup).</summary>
    internal static class FfmpegVideoThumbnailDecoder
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(45);
        private static string? _ffmpegPath;
        private static bool _resolved;

        public static DecodedThumbnail? TryGetThumbnail(string filePath, ILogger? logger = null)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            string? ffmpeg = ResolveFfmpegPath();
            if (ffmpeg == null)
            {
                return null;
            }

            string tempJpeg = Path.Combine(Path.GetTempPath(), "QuickMediaIngest", $"ff-{Guid.NewGuid():N}.jpg");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(tempJpeg)!);
                var psi = new ProcessStartInfo
                {
                    FileName = ffmpeg,
                    Arguments =
                        $"-hide_banner -loglevel error -y -ss 1 -i \"{filePath}\" -frames:v 1 -q:v 5 \"{tempJpeg}\"",
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

                if (!process.WaitForExit((int)Timeout.TotalMilliseconds))
                {
                    try
                    {
                        process.Kill(entireProcessTree: true);
                    }
                    catch
                    {
                        // ignore
                    }

                    return null;
                }

                if (process.ExitCode != 0 || !File.Exists(tempJpeg))
                {
                    return null;
                }

                byte[] jpeg = File.ReadAllBytes(tempJpeg);
                return JpegSofDimensionParser.TryCreate(jpeg);
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "ffmpeg video thumbnail failed for {Path}.", filePath);
                return null;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempJpeg))
                    {
                        File.Delete(tempJpeg);
                    }
                }
                catch
                {
                    // ignore
                }
            }
        }

        private static string? ResolveFfmpegPath()
        {
            if (_resolved)
            {
                return _ffmpegPath;
            }

            _resolved = true;
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var process = Process.Start(psi);
                if (process != null && process.WaitForExit(5000) && process.ExitCode == 0)
                {
                    _ffmpegPath = "ffmpeg";
                    return _ffmpegPath;
                }
            }
            catch
            {
                // not on PATH
            }

            _ffmpegPath = null;
            return null;
        }
    }
}
