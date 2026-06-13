#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QuickMediaIngest.Core
{
    internal static class FtpScanPlanner
    {
        public static async Task<List<FtpFolderScanPlan>> BuildScanPlanAsync(
            string host,
            int port,
            string user,
            string pass,
            string rootPath,
            bool includeSubfolders,
            int timeoutSeconds,
            CancellationToken cancellationToken,
            Action<FtpScanProgress>? progressCallback)
        {
            var queue = new Queue<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var plans = new List<FtpFolderScanPlan>();

            string normalizedRoot = FtpListingParser.NormalizeRemotePath(rootPath);
            queue.Enqueue(normalizedRoot);
            visited.Add(normalizedRoot);

            int processedFolders = 0;
            int discoveredFolders = 1;
            int discoveredFiles = 0;
            int skippedFolders = 0;

            while (queue.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string current = queue.Dequeue();

                progressCallback?.Invoke(new FtpScanProgress
                {
                    Phase = "Prescan",
                    CurrentFolder = current,
                    ProcessedFolders = processedFolders,
                    TotalFolders = discoveredFolders,
                    ProcessedFiles = 0,
                    TotalFiles = discoveredFiles,
                    SkippedFolders = skippedFolders,
                    Note = "Listing folder...",
                });

                List<FtpListingEntry> entries;
                bool folderSkipped = false;
                string skipReason = string.Empty;

                try
                {
                    entries = await FtpDirectoryClient.ListDirectoryEntriesAsync(
                        host,
                        port,
                        user,
                        pass,
                        current,
                        timeoutSeconds,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (string.Equals(current, normalizedRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Failed to list starting folder {current}: {ex.Message}", ex);
                    }

                    entries = new List<FtpListingEntry>();
                    folderSkipped = true;
                    skippedFolders++;
                    skipReason = $"Failed to list folder after retries: {ex.Message}";
                }

                var files = new List<FtpScanFile>();
                foreach (var entry in entries)
                {
                    if (!entry.IsDirectory)
                    {
                        if (!MediaExtensions.IsMediaFile(entry.Name))
                        {
                            continue;
                        }

                        files.Add(new FtpScanFile
                        {
                            FullPath = entry.FullPath,
                            Name = entry.Name,
                            Size = entry.Size,
                            Modified = entry.Modified,
                        });
                        continue;
                    }

                    if (!includeSubfolders)
                    {
                        continue;
                    }

                    string folder = FtpListingParser.NormalizeRemotePath(entry.FullPath);
                    if (visited.Add(folder))
                    {
                        queue.Enqueue(folder);
                        discoveredFolders++;
                    }
                }

                discoveredFiles += files.Count;
                processedFolders++;
                plans.Add(new FtpFolderScanPlan
                {
                    Folder = current,
                    Files = files,
                    IsSkipped = folderSkipped,
                    SkipReason = skipReason,
                });

                progressCallback?.Invoke(new FtpScanProgress
                {
                    Phase = "Prescan",
                    CurrentFolder = current,
                    ProcessedFolders = processedFolders,
                    TotalFolders = discoveredFolders,
                    ProcessedFiles = 0,
                    TotalFiles = discoveredFiles,
                    SkippedFolders = skippedFolders,
                    Note = skipReason,
                });
            }

            return plans;
        }
    }
}
