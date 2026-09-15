#nullable enable
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>About overlay: version, Venmo donate, fail-soft filename-version GitHub check.</summary>
    public static class IngestBenchAbout
    {
        public static string CurrentVersion
        {
            get
            {
                Version? v = Assembly.GetExecutingAssembly().GetName().Version;
                return (v ?? new Version(1, 5, 0)).ToString(3);
            }
        }

        public static string PackageType => OperatingSystem.IsLinux() ? "Deb" : "Portable";

        public static void RememberInstall(IngestBenchHost host)
        {
            var store = new FileUpdateDonateStore(host.Paths.AppDataRoot);
            UpdateDonatePreferences prefs = store.Load();
            if (UpdateDonateState.TryRecordFirstInstalledVersion(prefs, CurrentVersion))
            {
                store.Save(prefs);
            }
        }

        public static void Open(IngestBenchHost host)
        {
            host.Model.AboutVersion = CurrentVersion;
            host.Model.AboutUpdateStatus = string.Empty;
            host.Model.Nav.OpenAboutFromSettings();
        }

        public static bool OpenDonate() => ShellOpen.TryOpen(DonationLinks.Venmo);

        public static async Task CheckNowAsync(IngestBenchHost host, IngestBenchCopy copy)
        {
            host.Model.AboutUpdateStatus = copy.Get("About_Update_Checking");
            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.UserAgent.ParseAdd("QuickMediaIngest-Updater");
                var store = new FileUpdateDonateStore(host.Paths.AppDataRoot);
                var svc = new UpdateService(http, NullLogger<UpdateService>.Instance, store, new SystemClock());
                UpdateCheckResult result = await svc.CheckForUpdateAsync(force: true, packageType: PackageType);
                if (string.IsNullOrWhiteSpace(result.DownloadUrl))
                {
                    host.Model.AboutUpdateStatus = copy.Get("About_CheckUpdatesNow");
                    return;
                }

                host.Model.AboutUpdateStatus = result.RemoteVersionTag ?? result.DownloadUrl;
                ShellOpen.TryOpen(result.DownloadUrl);
            }
            catch
            {
                host.Model.AboutUpdateStatus = copy.Get("About_CheckUpdatesNow");
            }
        }
    }
}
