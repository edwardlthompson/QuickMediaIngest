#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Core
{
    internal sealed partial class FtpThumbnailPipeline
    {
        private readonly FtpFileDownloader _fileDownloader;
        private readonly FtpTieredPreviewLoader _tieredLoader;
        private readonly ILogger _logger;

        public FtpThumbnailPipeline(
            FtpFileDownloader fileDownloader,
            IThumbnailService thumbnailService,
            ILogger logger)
        {
            _fileDownloader = fileDownloader;
            _tieredLoader = new FtpTieredPreviewLoader(fileDownloader, thumbnailService, logger);
            _logger = logger;
        }

        public async Task<FtpThumbnailBatchResult> RunAsync(
            FtpEndpoint endpoint,
            IReadOnlyList<FtpThumbnailWorkItem> items,
            ThumbnailHints? hints,
            FtpThumbnailLoadOptions options,
            Func<FtpThumbnailProgress, Task>? onProgress,
            Func<FtpThumbnailItemResult, Task>? onItemCompleted,
            CancellationToken cancellationToken)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "QuickMediaIngest", "ftp-thumbs");
            Directory.CreateDirectory(tempDir);

            int downloadParallelism = Math.Max(1, Math.Min(options.DownloadParallelism, 6));
            using var decodeGate = new SemaphoreSlim(Math.Max(1, options.DecodeParallelism));
            using var fullDownloadGate = new SemaphoreSlim(2, 2);
            bool useFluentFtp = options.PerformanceMode is "Max" or "Ultra";

            int loadedCount = 0;
            int skippedCount = 0;
            int processedCount = 0;
            var results = new ConcurrentBag<FtpThumbnailItemResult>();
            var orderedItems = items.OrderBy(GetThumbnailPriority).ToList();

            await Parallel.ForEachAsync(
                orderedItems,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = downloadParallelism,
                    CancellationToken = cancellationToken
                },
                async (workItem, ct) =>
                {
                    string ext = Path.GetExtension(workItem.FileName);
                    if (string.IsNullOrWhiteSpace(ext))
                    {
                        ext = ".jpg";
                    }

                    string tempPath = Path.Combine(tempDir, $"{Guid.NewGuid():N}{ext}");
                    FtpThumbnailItemResult result;

                    try
                    {
                        DecodedThumbnail? cached = ThumbnailDiskCache.TryLoadFtp(
                            endpoint.Host,
                            endpoint.Port,
                            workItem.RemotePath,
                            workItem.FileSize);
                        DecodedThumbnail? thumb = cached;

                        if (thumb == null)
                        {
                            thumb = await LoadWithTieredFetchAsync(
                                endpoint,
                                workItem,
                                tempPath,
                                hints,
                                useFluentFtp,
                                decodeGate,
                                fullDownloadGate,
                                ct);
                        }

                        if (thumb != null)
                        {
                            if (cached == null)
                            {
                                ThumbnailDiskCache.TrySaveFtp(
                                    thumb,
                                    endpoint.Host,
                                    endpoint.Port,
                                    workItem.RemotePath,
                                    workItem.FileSize);
                            }

                            Interlocked.Increment(ref loadedCount);
                            result = new FtpThumbnailItemResult
                            {
                                ItemKey = workItem.ItemKey,
                                Thumbnail = thumb,
                                Status = ThumbnailPreviewStatus.Loaded
                            };
                        }
                        else
                        {
                            Interlocked.Increment(ref skippedCount);
                            result = new FtpThumbnailItemResult
                            {
                                ItemKey = workItem.ItemKey,
                                Status = ThumbnailPreviewStatus.Failed
                            };
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        Interlocked.Increment(ref skippedCount);
                        _logger.LogWarning(ex, "FTP thumbnail failed for {RemotePath}.", workItem.RemotePath);
                        result = new FtpThumbnailItemResult
                        {
                            ItemKey = workItem.ItemKey,
                            Status = ThumbnailPreviewStatus.Failed
                        };
                    }
                    finally
                    {
                        TryDeleteTemp(tempPath);
                    }

                    results.Add(result);
                    if (onItemCompleted != null)
                    {
                        await onItemCompleted(result);
                    }

                    int processed = Interlocked.Increment(ref processedCount);
                    if (onProgress != null)
                    {
                        await onProgress(new FtpThumbnailProgress
                        {
                            Processed = processed,
                            Total = orderedItems.Count,
                            CurrentRemotePath = workItem.RemotePath
                        });
                    }
                });

            return new FtpThumbnailBatchResult
            {
                LoadedCount = loadedCount,
                SkippedCount = skippedCount,
                Items = results.ToList()
            };
        }
    }
}
