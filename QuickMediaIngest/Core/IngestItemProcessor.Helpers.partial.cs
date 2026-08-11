#nullable enable
using System;
using System.IO;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Logging;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    internal static partial class IngestItemProcessor
    {
        internal static void TryDeletePartialDestination(string destPath, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(destPath) || !File.Exists(destPath))
            {
                return;
            }

            try
            {
                File.Delete(destPath);
                logger.LogDebug(
                    "Removed partial destination file {DestinationPath} after failed or canceled import.",
                    LogPathSanitizer.Local(destPath));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogDebug(ex, "Could not remove partial destination {DestinationPath}.", LogPathSanitizer.Local(destPath));
            }
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
