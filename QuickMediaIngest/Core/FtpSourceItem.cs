#nullable enable
namespace QuickMediaIngest
{
    /// <summary>Sidebar row type for an FTP source (single canonical type — use everywhere instead of duplicating).</summary>
    public class FtpSourceItem
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 21;
        public string User { get; set; } = "anonymous";
        public string Pass { get; set; } = "anonymous";
        public string RemoteFolder { get; set; } = "/DCIM";

        public override string ToString() => $"FTP: {Host} ({RemoteFolder})";
    }
}
