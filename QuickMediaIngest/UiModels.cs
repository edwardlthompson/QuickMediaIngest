using System;

namespace QuickMediaIngest
{
    // Lightweight UI-facing types to satisfy XAML type resolution when needed.
    public class FtpSourceItem
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 21;
        public string User { get; set; } = "anonymous";
        public string Pass { get; set; } = "anonymous";
        public string RemoteFolder { get; set; } = "/DCIM";

        public override string ToString() => $"FTP: {Host} ({RemoteFolder})";
    }

    public class UnifiedSourceItem
    {
        public override string ToString() => "Unified (SD + FTP)";
    }
}
