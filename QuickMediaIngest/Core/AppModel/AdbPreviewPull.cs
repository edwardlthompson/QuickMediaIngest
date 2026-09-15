#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Pulls a remote ADB file to a local temp path so Magick can decode a preview.</summary>
    public static class AdbPreviewPull
    {
        public static string? TryMaterialize(IAppPaths paths, ImportItem item)
        {
            if (!item.SourceId.StartsWith("adb:", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string serial = item.SourceId[4..].Trim();
            if (string.IsNullOrWhiteSpace(serial) || string.IsNullOrWhiteSpace(item.SourcePath)
                || !item.SourcePath.StartsWith('/'))
            {
                return null;
            }

            string ext = Path.GetExtension(item.FileName);
            if (string.IsNullOrEmpty(ext))
            {
                ext = Path.GetExtension(item.SourcePath);
            }

            string dest = Path.Combine(IngestBenchThumbs.CacheDir(paths), "adb-src", Guid.NewGuid().ToString("N") + ext);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                return TryPull(serial, item.SourcePath, dest) ? dest : null;
            }
            catch
            {
                TryDelete(dest);
                return null;
            }
        }

        internal static bool TryPull(string serial, string devicePath, string localPath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "adb",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                psi.ArgumentList.Add("-s");
                psi.ArgumentList.Add(serial);
                psi.ArgumentList.Add("pull");
                psi.ArgumentList.Add(devicePath);
                psi.ArgumentList.Add(localPath);
                using var process = Process.Start(psi);
                if (process == null || !process.WaitForExit(45_000) || process.ExitCode != 0)
                {
                    TryDelete(localPath);
                    return false;
                }

                return File.Exists(localPath) && new FileInfo(localPath).Length >= 64;
            }
            catch
            {
                TryDelete(localPath);
                return false;
            }
        }

        internal static void TryDelete(string path)
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
                // best effort
            }
        }
    }
}
