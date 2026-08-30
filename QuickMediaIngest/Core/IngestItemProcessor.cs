#nullable enable
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Logging;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    internal static partial class IngestItemProcessor
    {
        public static async Task ProcessOneAsync(
            ImportItem item,
            int itemIndex,
            int total,
            ItemGroup group,
            string targetDir,
            string namingTemplate,
            IngestOptions options,
            bool deleteAfterImport,
            IFileProvider provider,
            ILogger logger,
            Action<int, string>? progressChanged,
            Action<IngestProgressInfo>? itemProcessed,
            CancellationToken cancellationToken)
        {
            ImportByteProgressTracker? tracker = options.ByteProgressTracker;
            string sourceKey = item.SourcePath;
            long fileSizeBytes = Math.Max(0, item.FileSize);

            tracker?.RegisterFileStarted(sourceKey, fileSizeBytes);
            itemProcessed?.Invoke(BuildProgressInfo(group, targetDir, item, itemIndex, total, string.Empty, true, string.Empty, isStarted: true));

            string status = $"Copying {item.FileName} ({itemIndex}/{total})";
            progressChanged?.Invoke((itemIndex * 100) / total, status);

            string destFileName = IngestFileNaming.ResolveFileName(
                item,
                targetDir,
                namingTemplate,
                group.Title,
                itemIndex,
                options.DuplicateHandling,
                out bool skippedAsDuplicate);
            string destPath = string.IsNullOrEmpty(destFileName) ? string.Empty : Path.Combine(targetDir, destFileName);
            bool success = false;
            string errorMessage = string.Empty;

            try
            {
                if (skippedAsDuplicate)
                {
                    success = true;
                    errorMessage = "Skipped duplicate due to duplicate policy.";
                    tracker?.RegisterFileCompleted(sourceKey, fileSizeBytes, success);
                    itemProcessed?.Invoke(BuildProgressInfo(group, targetDir, item, itemIndex, total, destPath, success, errorMessage, isStarted: false));
                    return;
                }

                if (options.IsDryRun)
                {
                    success = true;
                    errorMessage = "Dry run (simulated)";
                    tracker?.RegisterFileCompleted(sourceKey, fileSizeBytes, success);
                    itemProcessed?.Invoke(BuildProgressInfo(group, targetDir, item, itemIndex, total, destPath, success, errorMessage, isStarted: false));
                    return;
                }

                IProgress<long>? copyProgress = tracker == null
                    ? null
                    : new Progress<long>(bytes => tracker.ReportBytes(sourceKey, bytes));

                await provider.CopyAsync(item.SourcePath, destPath, cancellationToken, copyProgress, fileSizeBytes);
                success = true;
                logger.LogDebug("Imported file {FileName} to {DestinationPath}.", item.FileName, destPath);

                if (success && File.Exists(destPath))
                {
                    if (!string.IsNullOrWhiteSpace(options.SecondaryDestinationRoot))
                    {
                        try
                        {
                            string relShootDir = Path.GetFileName(targetDir);
                            string secShootDir = Path.Combine(options.SecondaryDestinationRoot, relShootDir);
                            Directory.CreateDirectory(secShootDir);
                            string secDestPath = Path.Combine(secShootDir, Path.GetFileName(destPath));
                            File.Copy(destPath, secDestPath, overwrite: true);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Failed secondary 3-2-1 destination copy for {File}.", item.FileName);
                        }
                    }

                    if (options.WriteXmpSidecarsOnly || !string.IsNullOrWhiteSpace(options.CreatorStamp) || !string.IsNullOrWhiteSpace(options.CopyrightStamp))
                    {
                        MetadataKeywordWriter.WriteXmpSidecarMetadata(
                            destPath,
                            options.ApplyImportKeywords ? options.ImportKeywords : null,
                            options.CreatorStamp,
                            options.CopyrightStamp,
                            logger);
                    }
                    else if (options.ApplyImportKeywords || options.StripGpsAndPii)
                    {
                        if (options.ApplyImportKeywords && options.ImportKeywords is { Count: > 0 })
                        {
                            MetadataKeywordWriter.TryApplyKeywords(destPath, options.ImportKeywords, options.StripGpsAndPii, logger);
                        }
                        else if (options.StripGpsAndPii)
                        {
                            MetadataKeywordWriter.TryStripGpsAndPii(destPath, logger);
                        }
                    }
                }

                await TryDeleteSourceAfterImportAsync(
                    item, destPath, deleteAfterImport, success, provider, options, logger, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                TryDeletePartialDestination(destPath, logger);
                tracker?.RegisterFileCompleted(sourceKey, fileSizeBytes, success: false);
                throw;
            }
            catch (Exception ex)
            {
                if (IngestAlreadyImported.TryFindVerifiedDestination(
                        item,
                        targetDir,
                        namingTemplate,
                        group.Title,
                        itemIndex,
                        options,
                        logger,
                        out string existingPath))
                {
                    if (!string.Equals(destPath, existingPath, StringComparison.OrdinalIgnoreCase))
                    {
                        TryDeletePartialDestination(destPath, logger);
                    }

                    destPath = existingPath;
                    success = true;
                    errorMessage = "Already imported at destination; skipped re-copy.";
                    logger.LogInformation(
                        "Treating {FileName} as already imported at {DestinationPath} after copy failure: {Error}",
                        item.FileName,
                        LogPathSanitizer.Local(destPath),
                        ex.Message);

                    await TryDeleteSourceAfterImportAsync(
                        item, destPath, deleteAfterImport, success, provider, options, logger, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    TryDeletePartialDestination(destPath, logger);
                    errorMessage = ex.Message;
                    logger.LogError(
                        ex,
                        "Failed to import file {FileName} from {SourcePath} to {DestinationPath}.",
                        item.FileName,
                        LogPathSanitizer.Local(item.SourcePath),
                        LogPathSanitizer.Local(destPath));
                }
            }

            tracker?.RegisterFileCompleted(sourceKey, fileSizeBytes, success);
            itemProcessed?.Invoke(BuildProgressInfo(group, targetDir, item, itemIndex, total, destPath, success, errorMessage, isStarted: false));
        }
    }
}
