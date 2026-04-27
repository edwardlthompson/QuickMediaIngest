using System;

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

    /// <summary>Sidebar selection for unified SD + FTP merged browse mode.</summary>
    public class UnifiedSourceItem
    {
        public override string ToString() => "Unified (SD + FTP)";
    }

    /// <summary>FTP browse dialog folder row.</summary>
    public sealed class FtpFolderOption
    {
        public string Path { get; set; } = "/";
        public string Label { get; set; } = string.Empty;
    }

    /// <summary>Payload for naming-token ribbon insert/move.</summary>
    public sealed class TokenInsertPayload
    {
        public string Token { get; set; } = string.Empty;
        public int Index { get; set; } = -1;
        public bool FromSelected { get; set; }
    }
}
