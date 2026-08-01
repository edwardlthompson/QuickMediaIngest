#nullable enable

namespace QuickMediaIngest.Core
{
    /// <summary>Probes whether a remote directory or file exists on an ADB device.</summary>
    public interface IAdbPathProbe
    {
        bool DirectoryExists(string deviceSerial, string remoteDirectory);

        bool FileExists(string deviceSerial, string remoteFilePath);
    }
}
