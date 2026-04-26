#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Handles the ingestion of media files into the destination directory, raising progress events and logging results.
    /// </summary>
    public class IngestEngine
    {
        // Event for UI progress monitoring (Percent, Status Message)
        /// <summary>
        /// Occurs when the ingest progress changes (percent complete and status message).
        /// </summary>
        public event Action<int, string>? ProgressChanged;
        /// <summary>
        /// Occurs when an item is processed during ingest. Signature: public event Action<IngestProgressInfo>? ItemProcessed;
        /// </summary>
        public event Action<IngestProgressInfo>? ItemProcessed;

        private readonly IFileProvider _provider;
        private readonly ILogger<IngestEngine> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="IngestEngine"/> class.
        /// </summary>
        /// <param name="provider">File provider for copy/delete operations.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        public IngestEngine(IFileProvider provider, ILogger<IngestEngine> logger)
        {
            _provider = provider;
            _logger = logger;
        }

        /// <summary>
        /// Ingests all selected items in a group to the destination directory, applying the naming template.
        /// </summary>
        /// <param name="group">The group of items to ingest.</param>
        /// <param name="destinationRoot">Root directory for output.</param>
        /// <param name="namingTemplate">Naming template for output files.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task IngestGroupAsync(ItemGroup group, string destinationRoot, string namingTemplate, CancellationToken cancellationToken, bool deleteAfterImport = false)
        {
            if (group == null || group.Items.Count == 0) return;

            // Check if there are any selected items; skip group if none are selected
            var selectedItems = group.Items.Where(i => i.IsSelected).ToList();
            if (selectedItems.Count == 0) return;

            string folderName = GetTargetFolderName(group);
            string targetDir = Path.Combine(destinationRoot, folderName);

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            int total = selectedItems.Count;
            int current = 0;
            object progressLock = new object();

            _logger.LogInformation("Starting ingest for group {GroupTitle} with {FileCount} selected files into {DestinationRoot}.", group.Title, total, destinationRoot);

            await Parallel.ForEachAsync(selectedItems, new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = cancellationToken }, async (item, ct) =>
            {
                int itemIndex;
                lock (progressLock)
                {
                    current++;
                    itemIndex = current;
                }

                string status = $"Copying {item.FileName} ({itemIndex}/{total})";
                ProgressChanged?.Invoke((itemIndex * 100) / total, status);

                string destFileName = ResolveFileName(item, targetDir, namingTemplate, group.Title, itemIndex);
                string destPath = Path.Combine(targetDir, destFileName);
                bool success = false;
                string errorMessage = string.Empty;

                try
                {
                    await _provider.CopyAsync(item.SourcePath, destPath, ct);
                    success = true;
                    _logger.LogInformation("Imported file {FileName} to {DestinationPath}.", item.FileName, destPath);

                    // If delete-after-import is enabled, verify hash and size before deleting
                    if (deleteAfterImport && success && File.Exists(destPath))
                    {
                        try
                        {
                            var srcInfo = new FileInfo(item.SourcePath);
                            var destInfo = new FileInfo(destPath);
                            if (srcInfo.Length == destInfo.Length && ComputeSHA256(item.SourcePath) == ComputeSHA256(destPath))
                            {
                                await _provider.DeleteAsync(item.SourcePath, ct);
                                _logger.LogInformation("Deleted source file {SourcePath} after successful import and verification.", item.SourcePath);
                            }
                            else
                            {
                                _logger.LogWarning("Source and destination file mismatch (hash or size) for {FileName}. Skipping delete.", item.FileName);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error verifying or deleting source file {SourcePath} after import.", item.SourcePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                    _logger.LogError(ex, "Failed to import file {FileName} from {SourcePath} to {DestinationPath}.", item.FileName, item.SourcePath, destPath);
                }
                ItemProcessed?.Invoke(new IngestProgressInfo
                {
                    GroupTitle = string.IsNullOrWhiteSpace(group.Title) ? targetDir : group.Title,
                    GroupCurrent = itemIndex,
                    GroupTotal = total,
                    SourcePath = item.SourcePath,
                    FileName = item.FileName,
                    FileSizeBytes = item.FileSize,
                    Success = success,
                    ErrorMessage = errorMessage
                });
            }); // End of Parallel.ForEachAsync

            ProgressChanged?.Invoke(100, "Ingest Completed!");
            _logger.LogInformation("Completed ingest for group {GroupTitle}.", group.Title);
        }

        // Computes SHA256 hash of a file
        private static string ComputeSHA256(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hash = sha.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }

        private string GetTargetFolderName(ItemGroup group)
        {
            string start = group.StartDate.ToString("yyyy-MM-dd");
            string end = group.EndDate.ToString("yyyy-MM-dd");
            string name = group.Title;

            if (string.IsNullOrEmpty(name))
            {
                return start == end ? start : $"{start} to {end}";
            }

            return start == end ? $"{start}+{name}" : $"{start} to {end}+{name}";
        }

        /// <summary>
        /// Resolves the output file name for an import item using the specified template and group title.
        /// </summary>
        /// <param name="item">The import item.</param>
        /// <param name="targetDir">The target directory.</param>
        /// <param name="template">The naming template.</param>
        /// <param name="shootName">The group/shoot name.</param>
        /// <returns>The resolved file name.</returns>
        public string ResolveFileName(ImportItem item, string targetDir, string template, string shootName, int sequenceNumber = 1)
        {
            string ext = Path.GetExtension(item.FileName);
            string outputName = template;
            string safeShootName = SanitizeFileNamePart(string.IsNullOrWhiteSpace(shootName) ? "Shoot" : shootName);

            if (string.IsNullOrEmpty(outputName))
            {
                outputName = "[Date]_[Time]_[Original]"; // Default fallback
            }

                        // Replace Tokens
            outputName = outputName.Replace("[Date]", item.DateTaken.ToString("yyyy-MM-dd"));
            outputName = outputName.Replace("[Time]", item.DateTaken.ToString("HH-mm-ss"));
            outputName = outputName.Replace("[TimeMs]", item.DateTaken.ToString("HH-mm-ss-fff"));
            outputName = outputName.Replace("[YYYY]", item.DateTaken.ToString("yyyy"));
            outputName = outputName.Replace("[MM]", item.DateTaken.ToString("MM"));
            outputName = outputName.Replace("[DD]", item.DateTaken.ToString("dd"));
            outputName = outputName.Replace("[HH]", item.DateTaken.ToString("HH"));
            outputName = outputName.Replace("[mm]", item.DateTaken.ToString("mm"));
            outputName = outputName.Replace("[ss]", item.DateTaken.ToString("ss"));
            outputName = outputName.Replace("[fff]", item.DateTaken.ToString("fff"));
            outputName = outputName.Replace("[ShootName]", safeShootName);
            outputName = outputName.Replace("[Original]", Path.GetFileNameWithoutExtension(item.FileName));
            outputName = outputName.Replace("[Sequence]", sequenceNumber.ToString("D4"));
            outputName = outputName.Replace("[Ext]", ext.TrimStart('.'));

            string destFileName = $"{outputName}{ext}";
            string fullPath = Path.Combine(targetDir, destFileName);

            int counter = 1;
            while (File.Exists(fullPath))
            {
                destFileName = $"{outputName}_{counter:D2}{ext}";
                fullPath = Path.Combine(targetDir, destFileName);
                counter++;
            }

            return destFileName;
        }

        private static string SanitizeFileNamePart(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return value.Trim();
        }
    }

    public class IngestProgressInfo
    {
        public string GroupTitle { get; set; } = string.Empty;
        public int GroupCurrent { get; set; }
        public int GroupTotal { get; set; }
        public string SourcePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
