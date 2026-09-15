#nullable enable
using System.Diagnostics;
using System.IO;

namespace QuickMediaIngest.Core
{
    /// <summary>Open a path with the OS handler: xdg-open on Linux, Process.Start elsewhere.</summary>
    public static class ShellOpen
    {
        public static bool TryOpen(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            string folder = path.Trim();
            if (!Directory.Exists(folder) && File.Exists(folder))
            {
                folder = Path.GetDirectoryName(folder) ?? folder;
            }

            if (!Directory.Exists(folder))
            {
                return false;
            }

            try
            {
                if (OperatingSystem.IsLinux())
                {
                    return Start("xdg-open", folder) || Start("gio", "open", folder);
                }

                Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static ProcessStartInfo LinuxStart(string fileName, params string[] args)
        {
            var info = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
            };
            foreach (string arg in args)
            {
                info.ArgumentList.Add(arg);
            }

            return info;
        }

        private static bool Start(params string[] argv)
        {
            if (argv.Length == 0)
            {
                return false;
            }

            using Process? proc = Process.Start(LinuxStart(argv[0], argv[1..]));
            return proc != null;
        }
    }
}
