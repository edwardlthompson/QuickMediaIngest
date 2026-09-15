#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Add/test/browse FTP draft bound by the Avalonia overlay.</summary>
    public sealed partial class FtpDraft : ObservableObject
    {
        [ObservableProperty] private string host = string.Empty;
        [ObservableProperty] private string portText = "21";
        [ObservableProperty] private string user = "anonymous";
        [ObservableProperty] private string password = "anonymous";
        [ObservableProperty] private string remoteFolder = "/DCIM";
        [ObservableProperty] private string throttleKbpsText = "0";
        [ObservableProperty] private string browseListing = string.Empty;
        [ObservableProperty] private string status = string.Empty;
        [ObservableProperty] private string wifiBrand = "Sony";

        public int Port => int.TryParse(PortText, out int port) && port > 0 && port <= 65535 ? port : 21;

        public int ThrottleKbps => int.TryParse(ThrottleKbpsText, out int kbps) && kbps > 0 ? kbps : 0;

        public long ThrottleBytesPerSecond => ThrottleKbps <= 0 ? 0 : ThrottleKbps * 1024L;
    }

    public sealed class SavedFtpSource
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 21;
        public string User { get; set; } = string.Empty;
        public string RemoteFolder { get; set; } = "/DCIM";

        public override string ToString() => $"{Host}:{Port} {RemoteFolder}";
    }
}
