#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    public enum DuplicateHandlingMode
    {
        Suffix = 0,
        Skip = 1,
        OverwriteIfNewer = 2
    }

    public enum ImportVerificationMode
    {
        Fast = 0,
        Strict = 1
    }

    public sealed class IngestOptions
    {
        public DuplicateHandlingMode DuplicateHandling { get; set; } = DuplicateHandlingMode.Suffix;
        public ImportVerificationMode VerificationMode { get; set; } = ImportVerificationMode.Fast;
        /// <summary>
        /// When true, writes keywords (EXIF/XMP or sidecar) to each successfully copied destination file.
        /// </summary>
        public bool ApplyImportKeywords { get; set; }
        /// <summary>
        /// Keywords to embed (comma/semicolon separated upstream).
        /// </summary>
        public IReadOnlyList<string>? ImportKeywords { get; set; }

        /// <summary>Maximum concurrent copy operations for this ingest. 0 = derive from processor count (capped).</summary>
        public int MaxConcurrentFileCopies { get; set; }

        /// <summary>
        /// Optional pause between finishing one file and starting the next when running sequentially
        /// (helps flaky USB readers). Ignored unless copies run single-file or parallelism is 1.
        /// </summary>
        public int DelayBetweenFilesMilliseconds { get; set; }
    }

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
        public async Task IngestGroupAsync(
            ItemGroup group,
            string destinationRoot,
            string namingTemplate,
            CancellationToken cancellationToken,
            IngestOptions? options = null,
            bool deleteAfterImport = false)
        {
            if (group == null || group.Items.Count == 0) return;
            options ??= new IngestOptions();

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

            var wall = Stopwatch.StartNew();
            int parallelImports = options.MaxConcurrentFileCopies > 0
                ? Math.Clamp(options.MaxConcurrentFileCopies, 1, 16)
                : Math.Clamp(Environment.ProcessorCount, 1, 8);

            // Cool-down implies sequential ingest so ordering and delays stay predictable on flaky media.
            if (options.DelayBetweenFilesMilliseconds > 0)
            {
                parallelImports = 1;
            }

            async Task HandleOneItemAsync(ImportItem item, int itemIndex, CancellationToken ct)
            {
                string status = $"Copying {item.FileName} ({itemIndex}/{total})";
                ProgressChanged?.Invoke((itemIndex * 100) / total, status);

                string destFileName = ResolveFileName(item, targetDir, namingTemplate, group.Title, itemIndex, options.DuplicateHandling, out bool skippedAsDuplicate);
                string destPath = string.IsNullOrEmpty(destFileName) ? string.Empty : Path.Combine(targetDir, destFileName);
                bool success = false;
                string errorMessage = string.Empty;

                try
                {
                    if (skippedAsDuplicate)
                    {
                        success = true;
                        errorMessage = "Skipped duplicate due to duplicate policy.";
                        ItemProcessed?.Invoke(new IngestProgressInfo
                        {
                            GroupTitle = string.IsNullOrWhiteSpace(group.Title) ? targetDir : group.Title,
                            GroupCurrent = itemIndex,
                            GroupTotal = total,
                            SourcePath = item.SourcePath,
                            DestinationPath = string.Empty,
                            FileName = item.FileName,
                            FileSizeBytes = item.FileSize,
                            Success = true,
                            ErrorMessage = errorMessage
                        });
                        return;
                    }

                    await _provider.CopyAsync(item.SourcePath, destPath, ct);
                    success = true;
                    _logger.LogDebug("Imported file {FileName} to {DestinationPath}.", item.FileName, destPath);

                    if (success && File.Exists(destPath) && options.ApplyImportKeywords && options.ImportKeywords is { Count: > 0 })
                    {
                        MetadataKeywordWriter.TryApplyKeywords(destPath, options.ImportKeywords, _logger);
                    }

                    if (deleteAfterImport && success && File.Exists(destPath))
                    {
                        try
                        {
                            if (IsPostImportVerifiedForDelete(item, destPath, options, out string? verifyNote))
                            {
                                await _provider.DeleteAsync(item.SourcePath, ct);
                                _logger.LogInformation("Deleted source file {SourcePath} after successful import and verification.", item.SourcePath);
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "Source and destination did not pass post-import verification for {FileName}. {Details} Skipping delete.",
                                    item.FileName,
                                    verifyNote ?? string.Empty);
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
                    DestinationPath = destPath,
                    FileName = item.FileName,
                    FileSizeBytes = item.FileSize,
                    Success = success,
                    ErrorMessage = errorMessage
                });
            }

            if (parallelImports <= 1)
            {
                int seqIndex = 0;
                foreach (var item in selectedItems)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (seqIndex > 0 && options.DelayBetweenFilesMilliseconds > 0)
                    {
                        await Task.Delay(options.DelayBetweenFilesMilliseconds, cancellationToken);
                    }

                    seqIndex++;
                    lock (progressLock)
                    {
                        current++;
                    }

                    await HandleOneItemAsync(item, seqIndex, cancellationToken);
                }
            }
            else
            {
                await Parallel.ForEachAsync(selectedItems, new ParallelOptions { MaxDegreeOfParallelism = parallelImports, CancellationToken = cancellationToken }, async (item, ct) =>
                {
                    int itemIndex;
                    lock (progressLock)
                    {
                        current++;
                        itemIndex = current;
                    }

                    await HandleOneItemAsync(item, itemIndex, ct);
                });
            }

            ProgressChanged?.Invoke(100, "Ingest Completed!");
            wall.Stop();
            _logger.LogInformation(
                "Completed ingest for group {GroupTitle}. WallTimeMs={WallMs}, Parallelism={Parallelism}",
                group.Title,
                wall.Elapsed.TotalMilliseconds,
                parallelImports);
        }

        /// <summary>
        /// Decide whether it is safe to delete the source after copy. FTP and other non-local paths are not readable
        /// via <see cref="FileInfo"/> / <see cref="File.OpenRead"/> on Windows; use listing size vs destination size instead.
        /// </summary>
        private bool IsPostImportVerifiedForDelete(
            ImportItem item,
            string destPath,
            IngestOptions options,
            out string? verifyNote)
        {
            verifyNote = null;
            if (!File.Exists(destPath))
            {
                verifyNote = "Destination missing.";
                return false;
            }

            var destInfo = new FileInfo(destPath);

            // Remote listing path (FTP): never treat SourcePath as a local disk path.
            if (item.IsFtpSource)
            {
                if (item.FileSize != destInfo.Length)
                {
                    verifyNote = $"FTP listing size {item.FileSize} bytes vs destination {destInfo.Length} bytes.";
                    return false;
                }

                if (options.VerificationMode == ImportVerificationMode.Strict)
                {
                    // Cannot hash server bytes via System.IO; size agreement with the downloaded file is the practical gate.
                    _logger.LogDebug("Strict verification for FTP source uses size match only for {FileName}.", item.FileName);
                }

                return true;
            }

            // Local file that exists on disk: full Fast / Strict checks.
            if (File.Exists(item.SourcePath))
            {
                var srcInfo = new FileInfo(item.SourcePath);
                if (options.VerificationMode == ImportVerificationMode.Strict)
                {
                    bool ok = srcInfo.Length == destInfo.Length
                        && ComputeSHA256(item.SourcePath) == ComputeSHA256(destPath);
                    if (!ok)
                    {
                        verifyNote = "Strict local verify failed (size or SHA-256 mismatch).";
                    }

                    return ok;
                }

                if (srcInfo.Length != destInfo.Length)
                {
                    verifyNote = $"Local source size {srcInfo.Length} vs destination {destInfo.Length}.";
                    return false;
                }

                return true;
            }

            // Non-FTP path that does not resolve locally (edge cases): fall back to declared size vs destination.
            if (item.FileSize != destInfo.Length)
            {
                verifyNote = $"Declared source size {item.FileSize} vs destination {destInfo.Length}.";
                return false;
            }

            verifyNote = null;
            return true;
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
        public string ResolveFileName(
            ImportItem item,
            string targetDir,
            string template,
            string shootName,
            int sequenceNumber,
            DuplicateHandlingMode duplicateHandling,
            out bool skippedAsDuplicate)
        {
            skippedAsDuplicate = false;
            string ext = Path.GetExtension(item.FileName);
            string outputName = template;
            string safeShootName = SanitizeFileNamePart(string.IsNullOrWhiteSpace(shootName) ? "Shoot" : shootName);
            DateTime effectiveDateTaken = item.DateTaken;
            if ((template.Contains("[fff]", StringComparison.Ordinal) || template.Contains("[TimeMs]", StringComparison.Ordinal))
                && effectiveDateTaken.Millisecond == 0)
            {
                int syntheticMs = Math.Clamp(sequenceNumber % 1000, 1, 999);
                effectiveDateTaken = new DateTime(
                    effectiveDateTaken.Year,
                    effectiveDateTaken.Month,
                    effectiveDateTaken.Day,
                    effectiveDateTaken.Hour,
                    effectiveDateTaken.Minute,
                    effectiveDateTaken.Second,
                    syntheticMs,
                    effectiveDateTaken.Kind);
            }

            if (string.IsNullOrEmpty(outputName))
            {
                outputName = "[Date]_[Time]_[Original]"; // Default fallback
            }

                        // Replace Tokens
            outputName = outputName.Replace("[Date]", effectiveDateTaken.ToString("yyyy-MM-dd"));
            outputName = outputName.Replace("[Time]", effectiveDateTaken.ToString("HH-mm-ss"));
            outputName = outputName.Replace("[TimeMs]", effectiveDateTaken.ToString("HH-mm-ss-fff"));
            outputName = outputName.Replace("[YYYY]", effectiveDateTaken.ToString("yyyy"));
            outputName = outputName.Replace("[MM]", effectiveDateTaken.ToString("MM"));
            outputName = outputName.Replace("[DD]", effectiveDateTaken.ToString("dd"));
            outputName = outputName.Replace("[HH]", effectiveDateTaken.ToString("HH"));
            outputName = outputName.Replace("[mm]", effectiveDateTaken.ToString("mm"));
            outputName = outputName.Replace("[ss]", effectiveDateTaken.ToString("ss"));
            outputName = outputName.Replace("[fff]", effectiveDateTaken.ToString("fff"));
            outputName = outputName.Replace("[ShootName]", safeShootName);
            outputName = outputName.Replace("[Original]", Path.GetFileNameWithoutExtension(item.FileName));
            outputName = outputName.Replace("[Sequence]", sequenceNumber.ToString("D4"));
            outputName = outputName.Replace("[Ext]", ext.TrimStart('.'));

            string destFileName = $"{outputName}{ext}";
            string fullPath = Path.Combine(targetDir, destFileName);

            if (File.Exists(fullPath))
            {
                switch (duplicateHandling)
                {
                    case DuplicateHandlingMode.Skip:
                        skippedAsDuplicate = true;
                        return string.Empty;
                    case DuplicateHandlingMode.OverwriteIfNewer:
                        try
                        {
                            var dstInfo = new FileInfo(fullPath);
                            if (item.IsFtpSource)
                            {
                                // FTP path is not a local file; use scan metadata for "source" time.
                                if (item.DateTaken.ToUniversalTime() <= dstInfo.LastWriteTimeUtc)
                                {
                                    skippedAsDuplicate = true;
                                    return string.Empty;
                                }
                            }
                            else
                            {
                                var srcInfo = new FileInfo(item.SourcePath);
                                if (srcInfo.LastWriteTimeUtc <= dstInfo.LastWriteTimeUtc)
                                {
                                    skippedAsDuplicate = true;
                                    return string.Empty;
                                }
                            }
                        }
                        catch
                        {
                            skippedAsDuplicate = true;
                            return string.Empty;
                        }
                        return destFileName;
                }
            }

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
        /// <summary>
        /// Final on-disk destination path when the file was copied or attempted.
        /// </summary>
        public string DestinationPath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
