#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Scans FTP servers for directories and files, providing directory listing and parsing.
    /// </summary>
    public class FtpScanner : IFtpScanner
    {
        private readonly ILogger<FtpScanner> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpScanner"/> class.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output.</param>
        public FtpScanner(ILogger<FtpScanner> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Lists directories on an FTP server at the specified remote path.
        /// </summary>
        /// <param name="host">FTP server host.</param>
        /// <param name="port">FTP server port.</param>
        /// <param name="user">FTP username.</param>
        /// <param name="pass">FTP password.</param>
        /// <param name="remotePath">Remote path to list.</param>
        /// <param name="timeoutSeconds">Timeout in seconds.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of directory names.</returns>
        public async Task<List<string>> ListDirectoriesAsync(
            string host,
            int port,
            string user,
            string pass,
            string remotePath,
            int timeoutSeconds = 15,
            CancellationToken cancellationToken = default)
        {
            string normalizedPath = FtpListingParser.NormalizeRemotePath(remotePath);
            _logger.LogInformation("Listing FTP directories for {Host}:{Port}{RemotePath}.", host, port, normalizedPath);
            var entries = await FtpDirectoryClient.ListDirectoryEntriesAsync(
                host,
                port,
                user,
                pass,
                normalizedPath,
                timeoutSeconds,
                cancellationToken);

            return entries
                .Where(e => e.IsDirectory)
                .Select(e => e.FullPath)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<(bool Success, string Message)> TestConnectionAsync(
            string host,
            int port,
            string user,
            string pass,
            string remotePath,
            int timeoutSeconds = 15,
            CancellationToken cancellationToken = default)
        {
            string normalizedPath = FtpListingParser.NormalizeRemotePath(remotePath);
            _logger.LogInformation("Testing FTP connection to {Host}:{Port}{RemotePath}.", host, port, normalizedPath);

            try
            {
                _ = await FtpDirectoryClient.ListDirectoryEntriesAsync(
                    host,
                    port,
                    user,
                    pass,
                    normalizedPath,
                    timeoutSeconds,
                    cancellationToken);

                return (true, $"Connected to {host}:{port} and listed folder {normalizedPath}");
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("FTP connection test canceled for {Host}:{Port}{RemotePath}.", host, port, normalizedPath);
                return (false, "Connection was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FTP connection test failed for {Host}:{Port}{RemotePath}.", host, port, normalizedPath);
                return (false, ex.Message);
            }
        }

        public async Task<List<ImportItem>> ScanAsync(
            string host,
            int port,
            string user,
            string pass,
            string remotePath,
            bool includeSubfolders,
            int timeoutSeconds = 20,
            CancellationToken cancellationToken = default,
            Action<FtpScanProgress>? progressCallback = null)
        {
            var items = new List<ImportItem>();
            string normalizedPath = FtpListingParser.NormalizeRemotePath(remotePath);
            _logger.LogInformation("Starting FTP scan for {Host}:{Port}{RemotePath}. IncludeSubfolders={IncludeSubfolders}", host, port, normalizedPath, includeSubfolders);

            List<FtpFolderScanPlan> plans = await FtpScanPlanner.BuildScanPlanAsync(
                host,
                port,
                user,
                pass,
                normalizedPath,
                includeSubfolders,
                timeoutSeconds,
                cancellationToken,
                progressCallback);

            int totalFolders = plans.Count;
            int totalFiles = plans.Sum(p => p.Files.Count);
            int scannedFolders = 0;
            int scannedFiles = 0;
            int skippedFolders = plans.Count(p => p.IsSkipped);

            foreach (var plan in plans)
            {
                int currentFolderProcessedFiles = 0;
                int currentFolderTotalFiles = plan.Files.Count;

                foreach (var file in plan.Files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    items.Add(new ImportItem
                    {
                        SourcePath = file.FullPath,
                        FileName = file.Name,
                        FileSize = file.Size,
                        DateTaken = file.Modified,
                        IsVideo = MediaExtensions.IsVideoExtension(Path.GetExtension(file.Name)),
                        FileType = Path.GetExtension(file.Name).TrimStart('.').ToUpperInvariant()
                    });

                    scannedFiles++;
                    currentFolderProcessedFiles++;
                    if (scannedFiles == 1 || scannedFiles % 25 == 0 || scannedFiles == totalFiles)
                    {
                        progressCallback?.Invoke(new FtpScanProgress
                        {
                            Phase = "Scanning",
                            CurrentFolder = plan.Folder,
                            ProcessedFolders = scannedFolders,
                            TotalFolders = totalFolders,
                            ProcessedFiles = scannedFiles,
                            TotalFiles = totalFiles,
                            CurrentFolderProcessedFiles = currentFolderProcessedFiles,
                            CurrentFolderTotalFiles = currentFolderTotalFiles,
                            SkippedFolders = skippedFolders,
                            Note = plan.IsSkipped ? plan.SkipReason : string.Empty
                        });
                    }
                }

                scannedFolders++;
                progressCallback?.Invoke(new FtpScanProgress
                {
                    Phase = "Scanning",
                    CurrentFolder = plan.Folder,
                    ProcessedFolders = scannedFolders,
                    TotalFolders = totalFolders,
                    ProcessedFiles = scannedFiles,
                    TotalFiles = totalFiles,
                    CurrentFolderProcessedFiles = currentFolderProcessedFiles,
                    CurrentFolderTotalFiles = currentFolderTotalFiles,
                    SkippedFolders = skippedFolders,
                    Note = plan.IsSkipped ? plan.SkipReason : string.Empty
                });
            }

            _logger.LogInformation("Completed FTP scan for {Host}:{Port}{RemotePath}. Files={FileCount}, Folders={FolderCount}, SkippedFolders={SkippedFolders}", host, port, normalizedPath, items.Count, totalFolders, skippedFolders);
            return items;
        }
    }
}
