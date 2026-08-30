#nullable enable
namespace QuickMediaIngest.Core.CrashCapture;

public sealed class PendingCrash
{
    public string Fingerprint { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string Stack { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public string CreatedAtUtc { get; set; } = string.Empty;
}
