#nullable enable
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    internal static class IngestItemProcessor
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

                IProgress<long>? copyProgress = tracker == null
                    ? null
                    : new Progress<long>(bytes => tracker.ReportBytes(sourceKey, bytes));

                await provider.CopyAsync(item.SourcePath, destPath, cancellationToken, copyProgress);
                success = true;
                logger.LogDebug("Imported file {FileName} to {DestinationPath}.", item.FileName, destPath);

                if (success && File.Exists(destPath) && options.ApplyImportKeywords && options.ImportKeywords is { Count: > 0 })
                {
                    MetadataKeywordWriter.TryApplyKeywords(destPath, options.ImportKeywords, logger);
                }

                if (deleteAfterImport && success && File.Exists(destPath))
                {
                    try
                    {
                        if (IngestVerification.IsPostImportVerifiedForDelete(item, destPath, options, logger, out string? verifyNote))
                        {
                            await provider.DeleteAsync(item.SourcePath, cancellationToken);
                            logger.LogInformation(
                                "Deleted source file {SourcePath} after successful import and verification.",
                                item.SourcePath);
                        }
                        else
                        {
                            logger.LogWarning(
                                "Source and destination did not pass post-import verification for {FileName}. {Details} Skipping delete.",
                                item.FileName,
                                verifyNote ?? string.Empty);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error verifying or deleting source file {SourcePath} after import.", item.SourcePath);
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                logger.LogError(
                    ex,
                    "Failed to import file {FileName} from {SourcePath} to {DestinationPath}.",
                    item.FileName,
                    item.SourcePath,
                    destPath);
            }

            tracker?.RegisterFileCompleted(sourceKey, fileSizeBytes, success);
            itemProcessed?.Invoke(BuildProgressInfo(group, targetDir, item, itemIndex, total, destPath, success, errorMessage, isStarted: false));
        }

        private static IngestProgressInfo BuildProgressInfo(
            ItemGroup group,
            string targetDir,
            ImportItem item,
            int itemIndex,
            int total,
            string destPath,
            bool success,
            string errorMessage,
            bool isStarted) =>
            new()
            {
                GroupTitle = string.IsNullOrWhiteSpace(group.Title) ? targetDir : group.Title,
                GroupCurrent = itemIndex,
                GroupTotal = total,
                SourcePath = item.SourcePath,
                DestinationPath = destPath,
                FileName = item.FileName,
                FileSizeBytes = item.FileSize,
                Success = success,
                ErrorMessage = errorMessage,
                IsStarted = isStarted,
            };
    }
}
