#nullable enable
namespace QuickMediaIngest.Core
{
    public interface IFileProviderFactory
    {
        IFileProvider CreateLocalProvider();
        IFileProvider CreateFtpProvider(string host, int port, string user, string pass);
        IFileProvider CreateAdbProvider(string deviceSerial);
        IFileProvider CreateAdbRemappingProvider(string deviceSerial, string mediaRootPrefix);
    }
}
