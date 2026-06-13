#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Data.Models;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Scans local directories for importable items.
    /// </summary>
    public interface ILocalScanner
    {
        List<ImportItem> Scan(string sourcePath, bool includeSubfolders, Action<int, int>? folderProgressCallback = null);
    }

    /// <summary>
    /// Scans FTP servers for directories and files.
    /// </summary>
    public interface IFtpScanner
    {
        Task<List<string>> ListDirectoriesAsync(
            string host,
            int port,
            string user,
            string pass,
            string remotePath,
            int timeoutSeconds = 15,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string Message)> TestConnectionAsync(
            string host,
            int port,
            string user,
            string pass,
            string remotePath,
            int timeoutSeconds = 15,
            CancellationToken cancellationToken = default);

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
        BitmapSource? GetThumbnail(string filePath);
        BitmapSource? GetThumbnail(string filePath, ThumbnailHints? hints);
    }

    /// <summary>
    /// Reads metadata from import items.
    /// </summary>
    public interface IMetadataReader
    {
        void ReadMetadata(ImportItem item);
    }

    /// <summary>
    /// Filters import items using whitelist rules.
    /// </summary>
    public interface IWhitelistFilter
    {
        List<ImportItem> Filter(List<ImportItem> items, List<WhitelistRule> rules);
    }

    /// <summary>Result of an update check: <see cref="DownloadUrl"/> is set when a newer release is available.</summary>
    public readonly record struct UpdateCheckResult(string? DownloadUrl, string? RemoteVersionTag);

    /// <summary>
    /// Checks for application updates.
    /// </summary>
    public interface IUpdateService
    {
        Task<UpdateCheckResult> CheckForUpdateAsync(int intervalHours = 24, bool force = false, string packageType = "Portable");
    }

    /// <summary>
    /// Watches for device (volume) connections and disconnections.
    /// </summary>
    public interface IDeviceWatcher
    {
        event Action<string>? DeviceConnected;
        event Action<string>? DeviceDisconnected;
        void Start();
        void Stop();
    }
}
