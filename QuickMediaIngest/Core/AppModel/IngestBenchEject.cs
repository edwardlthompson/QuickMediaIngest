#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Linux unmount via gio/udisks plus leftover-file reminder after delete-after.</summary>
    public static class IngestBenchEject
    {
        public static Func<string, IReadOnlyList<string>, int>? RunProcess { get; set; }

        public static Func<string?, string?>? MountPointResolver { get; set; }

        public static bool HasLeftoverLocalFiles(IEnumerable<ItemGroup> groups, bool deleteAfterImport) =>
            deleteAfterImport
            && groups.SelectMany(g => g.Items)
                .Any(i => !i.IsFtpSource && !string.IsNullOrWhiteSpace(i.SourcePath) && File.Exists(i.SourcePath));

        public static bool IsSafeUnmountTarget(string? mount)
        {
            if (string.IsNullOrWhiteSpace(mount))
            {
                return false;
            }

            string full = Path.GetFullPath(mount).Replace('\\', '/').TrimEnd('/');
            if (full.Length == 0 || full == "/")
            {
                return false;
            }

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(home)
                && string.Equals(full, Path.GetFullPath(home).Replace('\\', '/').TrimEnd('/'), StringComparison.Ordinal))
            {
                return false;
            }

            return full == "/media" || full.StartsWith("/media/", StringComparison.Ordinal)
                || full == "/run/media" || full.StartsWith("/run/media/", StringComparison.Ordinal)
                || full == "/mnt" || full.StartsWith("/mnt/", StringComparison.Ordinal);
        }

        public static IReadOnlyList<(string File, string[] Args)> BuildUnmountCommands(string mountPoint) =>
            new[]
            {
                ("gio", new[] { "mount", "-u", mountPoint }),
                ("udisksctl", new[] { "unmount", "-p", mountPoint }),
            };

        public static string? ResolveMountPoint(string? sourcePath)
        {
            if (MountPointResolver is not null)
            {
                return MountPointResolver(sourcePath);
            }

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return null;
            }

            try
            {
                string full = Path.GetFullPath(sourcePath);
                if (File.Exists(full))
                {
                    full = Path.GetDirectoryName(full) ?? full;
                }

                if (!OperatingSystem.IsLinux() || !Directory.Exists(full))
                {
                    return Directory.Exists(full) ? full : null;
                }

                return ProbeFindmnt(full) ?? full;
            }
            catch
            {
                return null;
            }
        }

        public static bool TryUnmount(string? sourcePath)
        {
            string? mount = ResolveMountPoint(sourcePath);
            if (mount is null || !IsSafeUnmountTarget(mount))
            {
                return false;
            }

            foreach ((string file, string[] args) in BuildUnmountCommands(mount))
            {
                if (Invoke(file, args) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static Task AfterImportAsync(IngestBenchHost host, IReadOnlyList<ItemGroup> groups, IngestBenchCopy copy) =>
            host.Model.Prefs.EjectAfterImport ? EjectNowAsync(host, groups, copy) : Task.CompletedTask;

        public static async Task EjectNowAsync(IngestBenchHost host, IReadOnlyList<ItemGroup>? groups, IngestBenchCopy copy)
        {
            if (host.Model.IsImporting)
            {
                await host.Prompt.NotifyAsync(copy.Get("Msg_EjectBusy_Title"), copy.Get("Msg_EjectBusy_Body"));
                return;
            }

            IReadOnlyList<ItemGroup> list = groups ?? host.Groups;
            if (HasLeftoverLocalFiles(list, host.Model.DeleteAfterImport))
            {
                string body = copy.Get("Msg_EjectLeftover_Body");
                IngestBenchA11y.Announce(host.Model, body);
                await host.Prompt.NotifyAsync(copy.Get("Msg_EjectLeftover_Title"), body);
                return;
            }

            TryUnmount(host.ScanRoots.FirstOrDefault());
        }

        private static string? ProbeFindmnt(string full)
        {
            using var proc = Start("findmnt", "-n", "-o", "TARGET", "--target", full);
            if (proc is null)
            {
                return full;
            }

            string output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit(3000);
            return string.IsNullOrWhiteSpace(output) ? full : output.Split('\n')[0].Trim();
        }

        private static int Invoke(string file, IReadOnlyList<string> args)
        {
            if (RunProcess is not null)
            {
                return RunProcess(file, args);
            }

            try
            {
                using Process? proc = Start(file, args.ToArray());
                if (proc is null)
                {
                    return -1;
                }

                proc.WaitForExit(8000);
                return proc.HasExited ? proc.ExitCode : -1;
            }
            catch
            {
                return -1;
            }
        }

        private static Process? Start(string file, params string[] args)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = file,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                },
            };
            foreach (string arg in args)
            {
                proc.StartInfo.ArgumentList.Add(arg);
            }

            if (!proc.Start())
            {
                proc.Dispose();
                return null;
            }

            return proc;
        }
    }
}
