#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Scans local directories for importable media files.
    /// </summary>
    public class LocalScanner : ILocalScanner
    {
        private readonly ILogger<LocalScanner> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalScanner"/> class.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output.</param>
        public LocalScanner(ILogger<LocalScanner> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Scans the specified source path for importable files, optionally including subfolders.
        /// </summary>
        /// <param name="sourcePath">Root directory to scan.</param>
        /// <param name="includeSubfolders">Whether to include subfolders in the scan.</param>
        /// <param name="folderProgressCallback">Optional callback for folder scan progress.</param>
        /// <returns>List of discovered import items.</returns>
        public List<ImportItem> Scan(string sourcePath, bool includeSubfolders, Action<int, int>? folderProgressCallback = null)
        {
            var items = new List<ImportItem>();

            _logger.LogInformation("Starting local scan for {SourcePath}. IncludeSubfolders={IncludeSubfolders}", sourcePath, includeSubfolders);

            if (!Directory.Exists(sourcePath))
            {
                return items;
            }

            List<string> foldersToScan;
            try
            {
                foldersToScan = includeSubfolders
                    ? Directory.EnumerateDirectories(sourcePath, "*", SearchOption.AllDirectories).ToList()
                    : new List<string>();
            }
            catch
            {
                return items;
            }

            foldersToScan.Insert(0, sourcePath);
            int totalFolders = foldersToScan.Count;
            int scannedFolders = 0;

            foreach (var folder in foldersToScan)
            {
                IEnumerable<string> files;
                try
                {
                    files = Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly);
                }
                catch
                {
                    scannedFolders++;
                    folderProgressCallback?.Invoke(scannedFolders, totalFolders);
                    continue;
                }

                foreach (var file in files)
                {
                    FileInfo info = new FileInfo(file);
                    string ext = info.Extension.ToLowerInvariant();

                    // Skip non-media metadata files (CTG, DAT, etc.)
                    if (!IsMediaFile(ext)) continue;

                    items.Add(new ImportItem
                    {
                        SourcePath = info.FullName,
                        FileName = info.Name,
                        FileSize = info.Length,
                        DateTaken = info.LastWriteTime,
                        IsVideo = IsVideoFile(ext),
                        FileType = ext.TrimStart('.').ToUpper()
                    });
                }

                scannedFolders++;
                folderProgressCallback?.Invoke(scannedFolders, totalFolders);
            }

            _logger.LogInformation("Completed local scan for {SourcePath}. MediaFiles={FileCount}", sourcePath, items.Count);
            return items;
        }

        private static bool IsMediaFile(string ext)
        {
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif" ||
                   ext == ".bmp" || ext == ".tif" || ext == ".tiff" || ext == ".webp" ||
                   ext == ".dng" || ext == ".cr2" || ext == ".cr3" || ext == ".nef" ||
                   ext == ".arw" || ext == ".raf" || ext == ".orf" || ext == ".rw2" ||
                   ext == ".srw" || ext == ".heic" || ext == ".heif" ||
                   IsVideoFile(ext);
        }

        private static bool IsVideoFile(string ext)
        {
            return ext == ".mp4" || ext == ".mov" || ext == ".avi" || ext == ".mkv" ||
                   ext == ".mts" || ext == ".m2ts" || ext == ".mpg" || ext == ".mpeg";
        }
    }
}
