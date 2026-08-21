#nullable enable
using System;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Pure rules for the 24-hour update interval, dismiss, and once-per-version donate nudge.
    /// </summary>
    public static class UpdateDonateState
    {
        public static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

        public static bool ShouldCheckForUpdate(DateTimeOffset now, DateTimeOffset? lastCheck)
        {
            if (!lastCheck.HasValue)
            {
                return true;
            }

            return now - lastCheck.Value >= CheckInterval;
        }

        public static bool IsNewerProductVersion(Version remote, Version current) =>
            ReleaseAssetVersion.IsNewer(remote, current);

        public static bool ShouldPromptUpdate(Version? remote, Version current, string? dismissedProductVersion)
        {
            if (remote == null || !IsNewerProductVersion(remote, current))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(dismissedProductVersion))
            {
                return true;
            }

            if (!Version.TryParse(dismissedProductVersion, out Version? dismissed) || dismissed == null)
            {
                return true;
            }

            return !ReleaseAssetVersion.SameProduct(remote, dismissed);
        }

        /// <summary>
        /// First run (empty recorded version) must not nudge. Show only when the installed version changed
        /// and this version has not already been marked seen.
        /// </summary>
        public static bool ShouldShowDonateNudge(string currentVersion, string? recordedInstalledVersion, string? donateNudgeSeenForVersion)
        {
            if (string.IsNullOrWhiteSpace(currentVersion) || string.IsNullOrWhiteSpace(recordedInstalledVersion))
            {
                return false;
            }

            if (string.Equals(currentVersion, recordedInstalledVersion, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !string.Equals(donateNudgeSeenForVersion, currentVersion, StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryRecordFirstInstalledVersion(UpdateDonatePreferences prefs, string currentVersion)
        {
            if (prefs == null || string.IsNullOrWhiteSpace(currentVersion) || !string.IsNullOrWhiteSpace(prefs.RecordedInstalledVersion))
            {
                return false;
            }

            prefs.RecordedInstalledVersion = currentVersion;
            return true;
        }

        public static void MarkDonateNudgeSeen(UpdateDonatePreferences prefs, string currentVersion)
        {
            prefs.DonateNudgeSeenForVersion = currentVersion;
            prefs.RecordedInstalledVersion = currentVersion;
        }

        public static void DismissProductVersion(UpdateDonatePreferences prefs, string productVersion)
        {
            prefs.DismissedProductVersion = productVersion;
        }
    }
}
