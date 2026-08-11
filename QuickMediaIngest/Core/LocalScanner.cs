#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Logging;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Scans local directories for importable media files.
    /// </summary>
    public class LocalScanner : ILocalScanner
    {
        private readonly ILogger<LocalScanner> _logger;
        private readonly IMetadataReader _metadataReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalScanner"/> class.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output.</param>
        /// <param name="metadataReader">EXIF/DateTaken enrichment for local-readable files.</param>
        public LocalScanner(ILogger<LocalScanner> logger, IMetadataReader metadataReader)
        {
            _logger = logger;
            _metadataReader = metadataReader;
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

            _logger.LogInformation("Starting local scan for {SourcePath}. IncludeSubfolders={IncludeSubfolders}", LogPathSanitizer.Local(sourcePath), includeSubfolders);

            if (!Directory.Exists(sourcePath))
            {
                return items;
            }

            List<string> foldersToScan;
            try
            {
                foldersToScan = includeSubfolders
                    ? Directory.EnumerateDirectories(sourcePath, "*", SearchOption.AllDirectories)
                        .Where(d => !IsUnderAndroidTrashDirectory(d))
                        .ToList()
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
                    if (MediaExtensions.IsAndroidTrashOrNoise(info.Name))
                    {
                        continue;
                    }

                    string ext = info.Extension.ToLowerInvariant();

                    // Skip non-media metadata files (CTG, DAT, etc.)
                    if (!IsMediaFile(ext)) continue;

                    var item = new ImportItem
                    {
                        SourcePath = info.FullName,
                        FileName = info.Name,
                        FileSize = info.Length,
                        DateTaken = info.LastWriteTime,
                        IsVideo = MediaExtensions.IsVideoExtension(ext),
                        FileType = ext.TrimStart('.').ToUpper()
                    };
                    _metadataReader.ReadMetadata(item);
                    items.Add(item);
                }

                scannedFolders++;
                folderProgressCallback?.Invoke(scannedFolders, totalFolders);
            }

            _logger.LogInformation("Completed local scan for {SourcePath}. MediaFiles={FileCount}", LogPathSanitizer.Local(sourcePath), items.Count);
            return items;
        }

        private static bool IsUnderAndroidTrashDirectory(string directoryPath)
        {
            string[] parts = directoryPath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Any(MediaExtensions.IsAndroidTrashDirectory);
        }

        private static bool IsMediaFile(string ext) => MediaExtensions.IsMediaExtension(ext);
    }
}
