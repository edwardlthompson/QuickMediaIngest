#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>PTP/USB tether via gvfs gphoto2 mounts (libusb stack), not WPD.</summary>
    public static class IngestBenchPtp
    {
        public static void Open(IngestBenchHost host)
        {
            host.Model.PtpDevices.Clear();
            foreach (string root in ListRoots())
            {
                host.Model.PtpDevices.Add(root);
            }

            host.Model.ShowPtpPicker = true;
        }

        public static void Close(IngestBenchHost host) => host.Model.ShowPtpPicker = false;

        public static void Confirm(IngestBenchHost host)
        {
            string[] roots = host.Model.PtpDevices.ToArray();
            if (roots.Length == 0)
            {
                host.Model.ShowPtpPicker = false;
                return;
            }

            List<ImportItem> items = IngestBenchImport.CollectItems(roots, host.Model.ExcludedFolders);
            IngestBenchShoots.RebuildFromItems(host, items);
            host.Model.NoteSources(items.Count);
            host.Model.ShowPtpPicker = false;
        }

        public static IReadOnlyList<string> ListRoots()
        {
            var roots = new List<string>();
            string? runtime = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
            if (!string.IsNullOrWhiteSpace(runtime))
            {
                string gvfs = Path.Combine(runtime, "gvfs");
                if (Directory.Exists(gvfs))
                {
                    foreach (string dir in Directory.GetDirectories(gvfs))
                    {
                        string name = Path.GetFileName(dir);
                        if (name.Contains("gphoto2", StringComparison.OrdinalIgnoreCase)
                            || name.Contains("mtp", StringComparison.OrdinalIgnoreCase))
                        {
                            roots.Add(dir);
                        }
                    }
                }
            }

            foreach (string detected in DetectGphoto2Usb())
            {
                if (!roots.Contains(detected))
                {
                    roots.Add(detected);
                }
            }

            return roots;
        }

        public static async System.Threading.Tasks.Task<IReadOnlyList<ImportItem>> ScanAsync(string deviceId)
        {
            var scanner = new PtpTetherScanner();
            return await scanner.ScanPtpDeviceAsync(deviceId);
        }

        private static List<string> DetectGphoto2Usb()
        {
            var found = new List<string>();
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "gphoto2",
                    Arguments = "--auto-detect",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using Process? process = Process.Start(psi);
                if (process is null)
                {
                    return found;
                }

                if (!process.WaitForExit(3000))
                {
                    try
                    {
                        process.Kill(entireProcessTree: true);
                    }
                    catch
                    {
                        // ignore
                    }

                    return found;
                }

                string output = process.StandardOutput.ReadToEnd();
                foreach (string line in output.Split('\n'))
                {
                    if (line.Contains("usb:", StringComparison.OrdinalIgnoreCase))
                    {
                        found.Add(line.Trim());
                    }
                }
            }
            catch
            {
                // gphoto2 missing
            }

            return found;
        }
    }
}
