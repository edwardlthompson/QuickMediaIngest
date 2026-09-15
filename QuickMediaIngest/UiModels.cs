using System;

namespace QuickMediaIngest
{
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
