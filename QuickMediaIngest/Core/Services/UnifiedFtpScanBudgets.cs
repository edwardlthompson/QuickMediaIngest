#nullable enable
namespace QuickMediaIngest.Core.Services
{
    /// <summary>Timeouts for unified-merge FTP only (single-source browse keeps longer budgets).</summary>
    public static class UnifiedFtpScanBudgets
    {
        public const int ConnectProbeSeconds = 8;
        public const int ListingSeconds = 45;
    }
}
