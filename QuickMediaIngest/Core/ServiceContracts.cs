#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Data.Models;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Scans local directories for importable items.
    /// </summary>
    public interface ILocalScanner
    {
        /// <summary>
        /// Scans the specified source path for importable files.
        /// </summary>
        /// <param name="sourcePath">Root directory to scan.</param>
        /// <param name="includeSubfolders">Whether to include subfolders in the scan.</param>
        /// <param name="folderProgressCallback">Optional callback for folder scan progress.</param>
        /// <returns>List of discovered import items.</returns>
        List<ImportItem> Scan(string sourcePath, bool includeSubfolders, Action<int, int>? folderProgressCallback = null);
    }

    /// <summary>
    /// Scans FTP servers for directories and files.
    /// </summary>
    public interface IFtpScanner
    {
        /// <summary>
        /// Lists directories on an FTP server at the specified remote path.
        /// </summary>
        Task<List<string>> ListDirectoriesAsync(
            string host,
            int port,
            string user,
            string pass,
            string remotePath,
            int timeoutSeconds = 15,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tests the connection to an FTP server.
        /// </summary>
        Task<(bool Success, string Message)> TestConnectionAsync(
            string host,
            int port,
            string user,
            string pass,
            string remotePath,
            int timeoutSeconds = 15,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Scans the FTP server for importable items.
        /// </summary>
        Task<List<ImportItem>> ScanAsync(
            string host,
            int port,
            string user,
            string pass,
            string remotePath,
            bool includeSubfolders,
            int timeoutSeconds = 20,
            CancellationToken cancellationToken = default,
            Action<FtpScanProgress>? progressCallback = null);
    }

    /// <summary>
    /// Provides thumbnail images for files.
    /// </summary>
    public interface IThumbnailService
    {
        /// <summary>
        /// Gets a thumbnail image for the specified file path.
        /// </summary>
        BitmapSource? GetThumbnail(string filePath);

        /// <summary>
        /// Gets a thumbnail with optional load hints (e.g. defer expensive RAW shell work).
        /// </summary>
        BitmapSource? GetThumbnail(string filePath, ThumbnailHints? hints);
    }

    /// <summary>
    /// Reads metadata from import items.
    /// </summary>
    public interface IMetadataReader
    {
        /// <summary>
        /// Reads metadata from the specified import item and updates its properties.
        /// </summary>
        void ReadMetadata(ImportItem item);
    }

    /// <summary>
    /// Filters import items using whitelist rules.
    /// </summary>
    public interface IWhitelistFilter
    {
        /// <summary>
        /// Filters the provided items using the specified whitelist rules.
        /// </summary>
        List<ImportItem> Filter(List<ImportItem> items, List<WhitelistRule> rules);
    }

    /// <summary>Result of an update check: <see cref="DownloadUrl"/> is set when a newer release is available.</summary>
    public readonly record struct UpdateCheckResult(string? DownloadUrl, string? RemoteVersionTag);

    /// <summary>
    /// Checks for application updates.
    /// </summary>
    public interface IUpdateService
    {
        /// <summary>
        /// Checks for updates asynchronously.
        /// </summary>
        Task<UpdateCheckResult> CheckForUpdateAsync(int intervalHours = 24, bool force = false, string packageType = "Portable");
    }

    /// <summary>
    /// Watches for device (volume) connections and disconnections.
    /// </summary>
    public interface IDeviceWatcher
    {
        /// <summary>
        /// Occurs when a device (volume) is connected.
        /// </summary>
        event Action<string>? DeviceConnected;
        /// <summary>
        /// Occurs when a device (volume) is disconnected.
        /// </summary>
        event Action<string>? DeviceDisconnected;

        /// <summary>
        /// Starts watching for device connection and disconnection events.
        /// </summary>
        void Start();
        /// <summary>
        /// Stops watching for device events and releases resources.
        /// </summary>
        void Stop();
    }

    public interface IFileProviderFactory
    {
        IFileProvider CreateLocalProvider();
        IFileProvider CreateFtpProvider(string host, int port, string user, string pass);
        IFileProvider CreateAdbProvider(string deviceSerial);
    }

    public interface IIngestEngineFactory
    {
        IngestEngine Create(IFileProvider provider);
    }

    public sealed class FileProviderFactory : IFileProviderFactory
    {
        private readonly ILogger<LocalFileProvider> _localLogger;
        private readonly ILogger<FtpFileProvider> _ftpLogger;
        private readonly ILogger<AdbFileProvider> _adbLogger;

        public FileProviderFactory(ILogger<LocalFileProvider> localLogger, ILogger<FtpFileProvider> ftpLogger, ILogger<AdbFileProvider> adbLogger)
        {
            _localLogger = localLogger;
            _ftpLogger = ftpLogger;
            _adbLogger = adbLogger;
        }

        public IFileProvider CreateLocalProvider()
        {
            return new LocalFileProvider(_localLogger);
        }

        public IFileProvider CreateFtpProvider(string host, int port, string user, string pass)
        {
            return new FtpFileProvider(host, port, user, pass, _ftpLogger);
        }

        public IFileProvider CreateAdbProvider(string deviceSerial)
        {
            return new AdbFileProvider(deviceSerial, _adbLogger);
        }
    }

    public sealed class IngestEngineFactory : IIngestEngineFactory
    {
        private readonly ILogger<IngestEngine> _logger;

        public IngestEngineFactory(ILogger<IngestEngine> logger)
        {
            _logger = logger;
        }

        public IngestEngine Create(IFileProvider provider)
        {
            return new IngestEngine(provider, _logger);
        }
    }
}

namespace QuickMediaIngest.Data
{
    public interface IDatabaseService
    {
        DeviceConfig? GetDeviceConfig(string id);
        void SaveDeviceConfig(DeviceConfig config);
        List<WhitelistRule> GetWhitelist(string deviceId);
        void AddWhitelistRule(WhitelistRule rule);

        /// <summary>Runs occasional <c>VACUUM</c> to control DB file growth.</summary>
        void TryPeriodicVacuum(int minimumDaysBetweenRuns = 14);
    }
}