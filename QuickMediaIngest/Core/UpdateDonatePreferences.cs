#nullable enable
using System;

namespace QuickMediaIngest.Core
{
    /// <summary>Device-local donate/update prefs. Do not copy or peer-sync this file.</summary>
    public sealed class UpdateDonatePreferences
    {
        public DateTimeOffset? LastUpdateCheckUtc { get; set; }
        public string? DismissedProductVersion { get; set; }
        public string? RecordedInstalledVersion { get; set; }
        public string? DonateNudgeSeenForVersion { get; set; }
    }
}
