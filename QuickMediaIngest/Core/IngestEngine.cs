#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Logging;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Handles the ingestion of media files into the destination directory, raising progress events and logging results.
    /// </summary>
    public class IngestEngine
    {
        public event Action<int, string>? ProgressChanged;
        public event Action<IngestProgressInfo>? ItemProcessed;

        private readonly IFileProvider _provider;
        private readonly ILogger<IngestEngine> _logger;

        public IngestEngine(IFileProvider provider, ILogger<IngestEngine> logger)
        {
            _provider = provider;
            _logger = logger;
        }

        public async Task IngestGroupAsync(
            ItemGroup group,
            string destinationRoot,
            string namingTemplate,
            CancellationToken cancellationToken,
            IngestOptions? options = null,
            bool deleteAfterImport = false)
        {
            if (group == null || group.Items.Count == 0)
            {
                return;
            }

            options ??= new IngestOptions();

            var selectedItems = group.Items.Where(i => i.IsSelected).ToList();
            if (selectedItems.Count == 0)
            {
                return;
            }

            string folderName = GroupFolderNaming.GetTargetFolderName(group);
            string targetDir = Path.Combine(destinationRoot, folderName);

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            int total = selectedItems.Count;
            int current = 0;
            object progressLock = new object();

            _logger.LogInformation(
                "Starting ingest for group {GroupTitle} with {FileCount} selected files into {DestinationRoot}.",
                group.Title,
                total,
                LogPathSanitizer.Local(destinationRoot));

            var wall = Stopwatch.StartNew();
            int parallelImports = options.MaxConcurrentFileCopies > 0
                ? Math.Clamp(options.MaxConcurrentFileCopies, 1, 16)
                : Math.Clamp(Environment.ProcessorCount, 1, 8);

            if (options.DelayBetweenFilesMilliseconds > 0)
            {
                parallelImports = 1;
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

                    await IngestItemProcessor.ProcessOneAsync(
                        item,
                        seqIndex,
                        total,
                        group,
                        targetDir,
                        namingTemplate,
                        options,
                        deleteAfterImport,
                        _provider,
                        _logger,
                        ProgressChanged,
                        ItemProcessed,
                        cancellationToken);
                }
            }
            else
            {
                await Parallel.ForEachAsync(
                    selectedItems,
                    new ParallelOptions { MaxDegreeOfParallelism = parallelImports, CancellationToken = cancellationToken },
                    async (item, ct) =>
                    {
                        int itemIndex;
                        lock (progressLock)
                        {
                            current++;
                            itemIndex = current;
                        }

                        await IngestItemProcessor.ProcessOneAsync(
                            item,
                            itemIndex,
                            total,
                            group,
                            targetDir,
                            namingTemplate,
                            options,
                            deleteAfterImport,
                            _provider,
                            _logger,
                            ProgressChanged,
                            ItemProcessed,
                            ct);
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

        public string ResolveFileName(
            ImportItem item,
            string targetDir,
            string template,
            string shootName,
            int sequenceNumber,
            DuplicateHandlingMode duplicateHandling,
            out bool skippedAsDuplicate) =>
            IngestFileNaming.ResolveFileName(item, targetDir, template, shootName, sequenceNumber, duplicateHandling, out skippedAsDuplicate);
    }
}
