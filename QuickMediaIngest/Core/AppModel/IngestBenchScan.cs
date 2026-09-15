#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Drive picker, then scan only ticked local volumes plus ticked phones.</summary>
    public static class IngestBenchScan
    {
        public static void BeginPick(IngestBenchHost host, IEnumerable<string>? extraRoots = null)
        {
            IngestBenchAdb.Apply(host);
            ScanSourceStore.FileDto memory = ScanSourceStore.Load(host.Paths);
            host.Model.Volumes.Clear();
            foreach (VolumeChoice volume in MountEnumerator.ListVolumes(extraRoots))
            {
                volume.IsSelected = ScanSourceStore.IsSelected(volume.Path, volume.Label, memory);
                host.Model.Volumes.Add(volume);
            }

            string watch = host.Model.Prefs.WatchFolder?.Trim() ?? string.Empty;
            if (watch.Length > 0 && Directory.Exists(watch)
                && host.Model.Volumes.All(v => !string.Equals(v.Path, watch, StringComparison.Ordinal)))
            {
                var folder = new VolumeChoice(watch, Path.GetFileName(watch.TrimEnd(Path.DirectorySeparatorChar)), "Folder");
                bool seenFolder = memory.SeenPaths.Any(p => string.Equals(p, folder.Path, StringComparison.OrdinalIgnoreCase));
                folder.IsSelected = seenFolder
                    ? memory.SelectedPaths.Any(p => string.Equals(p, folder.Path, StringComparison.OrdinalIgnoreCase))
                    : true;
                host.Model.Volumes.Add(folder);
            }

            foreach (string serial in AdbDeviceProbe.ListDeviceSerials())
            {
                var phone = new VolumeChoice("adb:" + serial, serial + " DCIM", "ADB");
                bool seen = memory.SeenPaths.Any(p => string.Equals(p, phone.Path, StringComparison.OrdinalIgnoreCase));
                phone.IsSelected = seen
                    ? memory.SelectedPaths.Any(p => string.Equals(p, phone.Path, StringComparison.OrdinalIgnoreCase))
                    : true;
                host.Model.Volumes.Add(phone);

                var pictures = new VolumeChoice(IngestBenchAdb.PicturesVolumePath(serial), serial + " Pictures", "ADB");
                bool seenPics = memory.SeenPaths.Any(p => string.Equals(p, pictures.Path, StringComparison.OrdinalIgnoreCase));
                pictures.IsSelected = seenPics
                    && memory.SelectedPaths.Any(p => string.Equals(p, pictures.Path, StringComparison.OrdinalIgnoreCase));
                host.Model.Volumes.Add(pictures);
            }

            host.Model.ShowDrivePicker = true;
            host.Model.ImportStatus = "Choose drives to scan.";
        }

        public static void Remember(IngestBenchHost host) =>
            ScanSourceStore.Save(host.Paths, host.Model.Volumes);

        public static async Task RunSelectedAsync(IngestBenchHost host, CancellationToken ct = default)
        {
            IngestBenchAdb.Apply(host);
            IngestBenchActivity.Begin(host, IngestBenchActivity.Scan, "Scanning…");
            string[] roots = host.ScanRoots.Where(Directory.Exists).ToArray();
            IReadOnlyList<string> exclude = host.Model.ExcludedFolders.ToList();
            string[] dcim = host.Model.Volumes
                .Where(v => v.IsSelected && v.Path.StartsWith("adb:", StringComparison.OrdinalIgnoreCase)
                    && !IngestBenchAdb.IsPicturesVolume(v.Path))
                .Select(v => IngestBenchAdb.SerialOf(v.Path))
                .Where(s => s.Length > 0)
                .ToArray();
            string[] pictures = host.Model.Volumes
                .Where(v => v.IsSelected && IngestBenchAdb.IsPicturesVolume(v.Path))
                .Select(v => IngestBenchAdb.SerialOf(v.Path))
                .Where(s => s.Length > 0)
                .ToArray();
            (List<ImportItem> local, IngestBenchAdb.AdbScanResult adb) packed = await Task.Run(
                    () => ScanSelectedWork(host, roots, exclude, dcim, pictures, ct),
                    ct)
                .ConfigureAwait(true);
            var selected = host.Model.Volumes.Where(v => v.IsSelected).ToList();
            host.Post(() => ApplyMerged(host, selected, packed.local, packed.adb));
        }

        public static async Task AllAsync(
            IngestBenchHost host,
            IEnumerable<string>? extraRoots = null,
            CancellationToken ct = default)
        {
            BeginPick(host, extraRoots);
            Remember(host);
            host.ScanRoots = host.Model.Volumes.Where(v => v.IsSelected && Directory.Exists(v.Path)).Select(v => v.Path).ToArray();
            host.Model.ShowDrivePicker = false;
            await RunSelectedAsync(host, ct).ConfigureAwait(true);
        }

        public static void Refresh(IngestBenchHost host)
        {
            List<ImportItem> existing = host.Groups.SelectMany(g => g.Items).ToList();
            if (host.ScanRoots.Length == 0 && existing.Count == 0)
            {
                BeginPick(host);
                return;
            }

            List<ImportItem> adb = existing
                .Where(i => i.SourceId.StartsWith("adb:", StringComparison.OrdinalIgnoreCase))
                .ToList();
            List<ImportItem> local = IngestBenchImport.CollectItems(host.ScanRoots, host.Model.ExcludedFolders);
            ApplyItems(host, local.Concat(adb).ToList(), host.Model.Volumes.ToList());
        }

        internal static void ApplyMerged(
            IngestBenchHost host,
            IReadOnlyList<VolumeChoice> localVolumes,
            List<ImportItem> localItems,
            IngestBenchAdb.AdbScanResult adb)
        {
            var volumes = new List<VolumeChoice>();
            volumes.AddRange(localVolumes);
            foreach (VolumeChoice phone in adb.Volumes)
            {
                if (volumes.All(v => !string.Equals(v.Path, phone.Path, StringComparison.OrdinalIgnoreCase)))
                {
                    volumes.Add(phone);
                }
            }

            ApplyItems(host, localItems.Concat(adb.Items).ToList(), volumes);
            host.Model.ImportStatus = FormatStatus(localItems.Count, adb.Items.Count, adb.DeviceCount);
            host.RetargetWatch();
        }

        private static void ApplyItems(IngestBenchHost host, List<ImportItem> items, IReadOnlyList<VolumeChoice> volumes)
        {
            host.Model.Volumes.Clear();
            foreach (VolumeChoice volume in volumes)
            {
                host.Model.Volumes.Add(volume);
            }

            IngestBenchShoots.RebuildFromItems(host, items);
            host.Model.NoteSources(items.Count);
        }

        private static (List<ImportItem> local, IngestBenchAdb.AdbScanResult adb) ScanSelectedWork(
            IngestBenchHost host,
            string[] roots,
            IReadOnlyList<string> exclude,
            string[] dcim,
            string[] pictures,
            CancellationToken ct)
        {
            var local = new List<ImportItem>();
            int phonePart = dcim.Length + pictures.Length == 0 ? 0 : 1;
            int parts = Math.Max(1, roots.Length + phonePart);
            int step = 0;
            foreach (string root in roots)
            {
                step++;
                string leaf = Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (string.IsNullOrEmpty(leaf))
                {
                    leaf = root;
                }

                IngestBenchActivity.Report(host, step * 90 / parts, "Scanning " + leaf + "…");
                local.AddRange(IngestBenchImport.CollectItems(new[] { root }, exclude));
            }

            IngestBenchAdb.AdbScanResult adb = IngestBenchAdb.Collect(ct, dcim, pictures);
            if (phonePart == 1)
            {
                IngestBenchActivity.Report(host, 95, "Scanning phones…");
            }

            return (local, adb);
        }

        private static string FormatStatus(int localCount, int adbCount, int deviceCount)
        {
            if (localCount + adbCount == 0)
            {
                return deviceCount == 0
                    ? "No media on the drives you selected."
                    : $"ADB: {deviceCount} device(s), no media in selected folders.";
            }

            return $"Scanned {localCount + adbCount} files ({localCount} local, {adbCount} ADB).";
        }
    }
}
