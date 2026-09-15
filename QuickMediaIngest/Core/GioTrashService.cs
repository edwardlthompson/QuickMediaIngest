#nullable enable
using System;
using System.Diagnostics;
using System.IO;

namespace QuickMediaIngest.Core
{
    public interface ITrashService
    {
        bool TryTrash(string path);
    }

    /// <summary><c>gio trash</c> then unlink. Never used for uid-0 (caller must refuse root first).</summary>
    public sealed class GioTrashService : ITrashService
    {
        public bool TryTrash(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (OperatingSystem.IsLinux() && TryGioTrash(path))
            {
                return true;
            }

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return true;
                }

                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: false);
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static bool TryGioTrash(string path)
        {
            try
            {
                using var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "gio",
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                    },
                };
                proc.StartInfo.ArgumentList.Add("trash");
                proc.StartInfo.ArgumentList.Add("--");
                proc.StartInfo.ArgumentList.Add(path);
                if (!proc.Start())
                {
                    return false;
                }

                proc.WaitForExit(3000);
                return proc.ExitCode == 0 && !File.Exists(path) && !Directory.Exists(path);
            }
            catch
            {
                return false;
            }
        }
    }
}
