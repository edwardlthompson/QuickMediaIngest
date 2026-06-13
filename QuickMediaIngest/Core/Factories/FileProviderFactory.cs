#nullable enable
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core
{
    public sealed class FileProviderFactory : IFileProviderFactory
    {
        private readonly ILogger<LocalFileProvider> _localLogger;
        private readonly ILogger<FtpFileProvider> _ftpLogger;
        private readonly ILogger<AdbFileProvider> _adbLogger;

        public FileProviderFactory(
            ILogger<LocalFileProvider> localLogger,
            ILogger<FtpFileProvider> ftpLogger,
            ILogger<AdbFileProvider> adbLogger)
        {
            _localLogger = localLogger;
            _ftpLogger = ftpLogger;
            _adbLogger = adbLogger;
        }

        public IFileProvider CreateLocalProvider() => new LocalFileProvider(_localLogger);

        public IFileProvider CreateFtpProvider(string host, int port, string user, string pass) =>
            new FtpFileProvider(host, port, user, pass, _ftpLogger);

        public IFileProvider CreateAdbProvider(string deviceSerial) =>
            new AdbFileProvider(deviceSerial, _adbLogger);
    }
}
