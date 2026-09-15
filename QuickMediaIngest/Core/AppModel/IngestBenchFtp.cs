#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using QuickMediaIngest.Core.Ftp;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Add/test/browse FTP, libsecret+0600 store, bandwidth cap for the ingest-bench.</summary>
    public static class IngestBenchFtp
    {
        public static void Open(IngestBenchHost host) => host.Model.ShowFtpEditor = true;

        public static void Close(IngestBenchHost host)
        {
            host.Model.ShowFtpEditor = false;
            host.Model.ShowFtpThrottle = false;
            host.Model.ShowFtpFailure = false;
        }

        public static void DismissFailure(IngestBenchHost host) => host.Model.ShowFtpFailure = false;

        public static void OpenThrottle(IngestBenchHost host) => host.Model.ShowFtpThrottle = true;

        public static IFtpCredentialStore Store(IAppPaths paths) =>
            OperatingSystem.IsLinux() ? new SecretServiceFtpCredentialStore(paths) : new FileFtpCredentialStore(paths);

        public static FtpBandwidthThrottler CreateThrottler(FtpDraft draft) =>
            new(draft.ThrottleBytesPerSecond);

        public static void Load(IngestBenchHost host)
        {
            host.Model.FtpSources.Clear();
            foreach (FtpSourceFile row in ReadFile(host.Paths))
            {
                host.Model.FtpSources.Add(ToItem(row));
            }
        }

        public static void Save(IngestBenchHost host)
        {
            FtpDraft draft = host.Model.Ftp;
            string hostName = FtpHostNormalizer.Normalize(draft.Host);
            if (string.IsNullOrWhiteSpace(hostName))
            {
                draft.Status = "Host required";
                return;
            }

            string folder = FtpPathNormalizer.Normalize(draft.RemoteFolder);
            Store(host.Paths).WritePassword(hostName, draft.Port, draft.User, draft.Password);
            var rows = ReadFile(host.Paths);
            rows.RemoveAll(r =>
                string.Equals(FtpHostNormalizer.Normalize(r.Host), hostName, StringComparison.OrdinalIgnoreCase)
                && r.Port == draft.Port);
            rows.Add(new FtpSourceFile
            {
                Host = hostName,
                Port = draft.Port,
                User = draft.User,
                RemoteFolder = folder,
                ThrottleKbps = draft.ThrottleKbps,
            });
            WriteFile(host.Paths, rows);
            Load(host);
            draft.RemoteFolder = folder;
            draft.Status = "Saved";
            host.Model.ShowFtpEditor = false;
        }

        public static void ApplyWifiPreset(IngestBenchHost host)
        {
            if (!Enum.TryParse(host.Model.Ftp.WifiBrand, ignoreCase: true, out CameraBrand brand))
            {
                return;
            }

            CameraWifiPreset? preset = CameraWifiProfilePresets.FindPresetForBrand(brand);
            if (preset is null)
            {
                return;
            }

            host.Model.Ftp.PortText = preset.DefaultPort.ToString();
            host.Model.Ftp.RemoteFolder = preset.StandardScanPaths.Count > 1
                ? preset.StandardScanPaths[1]
                : preset.StandardScanPaths[0];
            host.Model.Ftp.Status = preset.Name;
        }

        public static async Task TestAsync(IngestBenchHost host, IFtpScanner? scanner = null, CancellationToken ct = default)
        {
            FtpDraft draft = host.Model.Ftp;
            (bool ok, string message) = await (scanner ?? DefaultScanner()).TestConnectionAsync(
                FtpHostNormalizer.Normalize(draft.Host),
                draft.Port,
                draft.User,
                draft.Password,
                FtpPathNormalizer.Normalize(draft.RemoteFolder),
                timeoutSeconds: 8,
                ct);
            draft.Status = message;
            if (!ok)
            {
                host.Model.ShowFtpFailure = true;
                draft.Status = "Can't reach the camera folder. Try /DCIM.";
                draft.BrowseListing = message;
                await host.Prompt.NotifyAsync("FTP", message);
            }
        }

        public static async Task BrowseAsync(IngestBenchHost host, IFtpScanner? scanner = null, CancellationToken ct = default)
        {
            FtpDraft draft = host.Model.Ftp;
            List<string> dirs = await (scanner ?? DefaultScanner()).ListDirectoriesAsync(
                FtpHostNormalizer.Normalize(draft.Host),
                draft.Port,
                draft.User,
                draft.Password,
                FtpPathNormalizer.Normalize(draft.RemoteFolder),
                timeoutSeconds: 8,
                ct);
            draft.BrowseListing = dirs.Count == 0 ? "(empty)" : string.Join("\n", dirs);
            draft.Status = dirs.Count == 0 ? "No folders" : $"{dirs.Count} folder(s)";
        }

        private static IFtpScanner DefaultScanner() => new FtpScanner(NullLogger<FtpScanner>.Instance);

        private static string PathFor(IAppPaths paths) => System.IO.Path.Combine(paths.AppDataRoot, "ftp-sources.json");

        private static List<FtpSourceFile> ReadFile(IAppPaths paths)
        {
            try
            {
                string path = PathFor(paths);
                if (!File.Exists(path))
                {
                    return new List<FtpSourceFile>();
                }

                return JsonSerializer.Deserialize<List<FtpSourceFile>>(File.ReadAllText(path)) ?? new List<FtpSourceFile>();
            }
            catch
            {
                return new List<FtpSourceFile>();
            }
        }

        private static void WriteFile(IAppPaths paths, List<FtpSourceFile> rows)
        {
            string path = PathFor(paths);
            string? dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(path, JsonSerializer.Serialize(rows));
        }

        private static SavedFtpSource ToItem(FtpSourceFile row) => new()
        {
            Host = row.Host,
            Port = row.Port,
            User = row.User,
            RemoteFolder = row.RemoteFolder,
        };

        private sealed class FtpSourceFile
        {
            public string Host { get; set; } = string.Empty;
            public int Port { get; set; } = 21;
            public string User { get; set; } = string.Empty;
            public string RemoteFolder { get; set; } = "/DCIM";
            public int ThrottleKbps { get; set; }
        }
    }
}
