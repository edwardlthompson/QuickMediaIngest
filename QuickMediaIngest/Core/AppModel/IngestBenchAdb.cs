#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Prefer-ADB when <c>adb</c> is on PATH; scan every connected serial; drop FTP/ADB aliases.</summary>
    public static class IngestBenchAdb
    {
        public const string PicturesSuffix = "/pictures";

        public static bool IsPicturesVolume(string path) =>
            path.StartsWith("adb:", StringComparison.OrdinalIgnoreCase)
            && path.EndsWith(PicturesSuffix, StringComparison.OrdinalIgnoreCase);

        public static string SerialOf(string volumePath)
        {
            string rest = volumePath.StartsWith("adb:", StringComparison.OrdinalIgnoreCase)
                ? volumePath[4..]
                : volumePath;
            int slash = rest.IndexOf('/');
            return slash < 0 ? rest : rest[..slash];
        }

        public static string PicturesVolumePath(string serial) => "adb:" + serial + PicturesSuffix;

        public static void Apply(IngestBenchHost host)
        {
            host.Model.AdbAvailable = AdbDeviceProbe.IsAdbAvailable();
        }

        public static async Task<int> ScanConnectedAsync(IngestBenchHost host, CancellationToken ct = default)
        {
            Apply(host);
            AdbScanResult result = await Task.Run(() => Collect(ct), ct).ConfigureAwait(true);
            host.Post(() => ApplyResult(host, result));
            return result.Items.Count;
        }

        internal static AdbScanResult Collect(
            CancellationToken ct = default,
            IReadOnlyList<string>? dcimSerials = null,
            IReadOnlyList<string>? picturesSerials = null)
        {
            IReadOnlyList<string> dcim = dcimSerials ?? AdbDeviceProbe.ListDeviceSerials();
            IReadOnlyList<string> pictures = picturesSerials ?? Array.Empty<string>();
            var items = new List<ImportItem>();
            var volumes = new List<VolumeChoice>();
            int devices = dcim.Concat(pictures).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            if (devices == 0)
            {
                return new AdbScanResult(0, items, volumes, "No ADB devices.");
            }

            var scanner = new AdbMediaScanner(NullLogger<AdbMediaScanner>.Instance);
            var probe = new AdbShellPathProbe();
            foreach (string serial in dcim)
            {
                ct.ThrowIfCancellationRequested();
                volumes.Add(new VolumeChoice("adb:" + serial, serial, "ADB") { IsSelected = true });
                AppendFolder(serial, "/DCIM", items, scanner, probe, ct);
            }

            foreach (string serial in pictures)
            {
                ct.ThrowIfCancellationRequested();
                AppendFolder(serial, "/Pictures", items, scanner, probe, ct);
            }

            string status = items.Count == 0
                ? $"ADB: {devices} device(s), no media in selected folders."
                : $"ADB: {items.Count} files from {devices} device(s).";
            return new AdbScanResult(devices, items, volumes, status);
        }

        private static void AppendFolder(
            string serial,
            string folder,
            List<ImportItem> items,
            AdbMediaScanner scanner,
            IAdbPathProbe probe,
            CancellationToken ct)
        {
            AdbTransferSession? session = AdbTransferEligibility.TryResolveForSerial(serial, folder, probe);
            if (session is null && folder == "/DCIM")
            {
                session = AdbTransferEligibility.TryResolveForSerial(serial, "/DCIM/Camera", probe);
            }

            if (session is null)
            {
                return;
            }

            List<ImportItem>? found = scanner.ScanAsync(session.Value, folder, includeSubfolders: true, ct)
                .GetAwaiter()
                .GetResult();
            if (found is null || found.Count == 0)
            {
                return;
            }

            foreach (ImportItem item in found)
            {
                item.SourceId = "adb:" + serial;
                item.SourcePath = AdbAndroidPath.ToDevicePath(session.Value.MediaRootPrefix, item.SourcePath);
            }

            items.AddRange(found);
        }

        internal static void ApplyResult(IngestBenchHost host, AdbScanResult result)
        {
            host.Model.Volumes.Clear();
            foreach (VolumeChoice volume in result.Volumes)
            {
                host.Model.Volumes.Add(volume);
            }

            IngestBenchShoots.RebuildFromItems(host, result.Items.ToList());
            host.Model.NoteSources(result.Items.Count);
            host.Model.ImportStatus = result.Status;
        }

        public static void DedupeGroups(IngestBenchHost host)
        {
            if (!host.Model.PreferAdb || host.Groups.Count == 0)
            {
                return;
            }

            var items = host.Groups.SelectMany(g => g.Items).ToList();
            var unique = FtpAdbAliasFilter.DeduplicateDualFtpAliases(items);
            if (unique.Count == items.Count)
            {
                return;
            }

            host.Groups.Clear();
            host.Groups.AddRange(new GroupBuilder().BuildGroups(unique, IngestBenchShoots.Gap));
            IngestBenchShoots.ApplyFilter(host);
        }

        internal readonly record struct AdbScanResult(
            int DeviceCount,
            IReadOnlyList<ImportItem> Items,
            IReadOnlyList<VolumeChoice> Volumes,
            string Status);
    }
}
