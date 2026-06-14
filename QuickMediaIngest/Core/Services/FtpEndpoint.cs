#nullable enable

namespace QuickMediaIngest.Core.Services
{
    /// <summary>FTP connection details for Core services (no UI types).</summary>
    public sealed record FtpEndpoint(string Host, int Port, string User, string Pass);
}
