#nullable enable
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core
{
    public sealed class FileProviderFactory : IFileProviderFactory
    {
        private readonly ILogger<LocalFileProvider> _localLogger;
        private readonly ILogger<FtpFileProvider> _ftpLogger;
        private readonly ILogger<AdbFileProvider> _adbLogger;
        private readonly IAdbPathProbe _adbPathProbe;

        public FileProviderFactory(
            ILogger<LocalFileProvider> localLogger,
            ILogger<FtpFileProvider> ftpLogger,
            ILogger<AdbFileProvider> adbLogger,
            IAdbPathProbe adbPathProbe)
        {
            _localLogger = localLogger;
            _ftpLogger = ftpLogger;
            _adbLogger = adbLogger;
            _adbPathProbe = adbPathProbe;
        }

        public IFileProvider CreateLocalProvider() => new LocalFileProvider(_localLogger);

        public IFileProvider CreateFtpProvider(string host, int port, string user, string pass) =>
            new FtpFileProvider(host, port, user, pass, _ftpLogger);

        public IFileProvider CreateAdbProvider(string deviceSerial) =>
            new AdbFileProvider(deviceSerial, _adbLogger);

        public IFileProvider CreateAdbRemappingProvider(string deviceSerial, string mediaRootPrefix) =>
            new RemappingFileProvider(
                CreateAdbProvider(deviceSerial),
                mediaRootPrefix,
                _adbPathProbe,
                deviceSerial);
    }
}
