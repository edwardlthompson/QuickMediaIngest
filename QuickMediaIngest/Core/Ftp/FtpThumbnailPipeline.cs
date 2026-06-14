#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Core
{
    internal sealed class FtpThumbnailPipeline
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
                        BitmapSource? cached = ThumbnailDiskCache.TryLoadFtp(
                            endpoint.Host,
                            endpoint.Port,
                            workItem.RemotePath,
                            workItem.FileSize);
                        BitmapSource? thumb = cached;

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

        private async Task<BitmapSource?> LoadWithTieredFetchAsync(
            FtpEndpoint endpoint,
            FtpThumbnailWorkItem workItem,
            string tempPath,
            ThumbnailHints? hints,
            bool useFluentFtp,
            SemaphoreSlim decodeGate,
            SemaphoreSlim fullDownloadGate,
            CancellationToken cancellationToken)
        {
            if (MediaExtensions.IsRawExtension(Path.GetExtension(workItem.FileName)))
            {
                BitmapSource? sibling = await TryLoadSiblingPreviewAsync(
                    endpoint,
                    workItem,
                    tempPath,
                    hints,
                    useFluentFtp,
                    decodeGate,
                    cancellationToken);
                if (sibling != null)
                {
                    return sibling;
                }
            }

            BitmapSource? preview = await TryTieredDownloadAndDecodeAsync(
                endpoint,
                workItem.RemotePath,
                workItem.FileName,
                tempPath,
                hints,
                useFluentFtp,
                decodeGate,
                cancellationToken);

            if (preview != null || !ShouldTryFullDownload(workItem))
            {
                return preview;
            }

            bool full = false;
            await fullDownloadGate.WaitAsync(cancellationToken);
            try
            {
                full = await _fileDownloader.TryDownloadAsync(
                    endpoint.Host,
                    endpoint.Port,
                    endpoint.User,
                    endpoint.Pass,
                    workItem.RemotePath,
                    tempPath,
                    45,
                    cancellationToken);
            }
            finally
            {
                fullDownloadGate.Release();
            }

            if (!full)
            {
                return null;
            }

            await decodeGate.WaitAsync(cancellationToken);
            try
            {
                return _tieredLoader.TryDecodeDownloaded(
                    workItem.FileName,
                    tempPath,
                    hints,
                    FtpPreviewDecodeMode.CompleteFile);
            }
            finally
            {
                decodeGate.Release();
            }
        }

        private async Task<BitmapSource?> TryTieredDownloadAndDecodeAsync(
            FtpEndpoint endpoint,
            string remotePath,
            string fileName,
            string tempPath,
            ThumbnailHints? hints,
            bool useFluentFtp,
            SemaphoreSlim decodeGate,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<long> tiers = FtpPreviewDownloadLimits.GetPreviewByteTiers(fileName);
            FtpStreamingDownloader? fluent = useFluentFtp
                ? FtpStreamingDownloader.GetOrCreate(endpoint, poolSize: 3, _logger)
                : null;

            for (int tierIndex = 0; tierIndex < tiers.Count; tierIndex++)
            {
                long maxBytes = tiers[tierIndex];
                FtpPreviewDecodeMode decodeMode = tierIndex < tiers.Count - 1
                    ? FtpPreviewDecodeMode.TieredPartial
                    : FtpPreviewDecodeMode.TieredFinalCap;

                cancellationToken.ThrowIfCancellationRequested();
                TryDeleteTemp(tempPath);

                bool downloaded = fluent != null
                    ? await fluent.TryDownloadCappedAsync(remotePath, tempPath, maxBytes, cancellationToken)
                    : await _fileDownloader.TryDownloadCappedAsync(
                        endpoint.Host,
                        endpoint.Port,
                        endpoint.User,
                        endpoint.Pass,
                        remotePath,
                        tempPath,
                        maxBytes,
                        20,
                        cancellationToken);

                if (!downloaded && fluent != null)
                {
                    downloaded = await _fileDownloader.TryDownloadCappedAsync(
                        endpoint.Host,
                        endpoint.Port,
                        endpoint.User,
                        endpoint.Pass,
                        remotePath,
                        tempPath,
                        maxBytes,
                        20,
                        cancellationToken);
                }

                if (!downloaded)
                {
                    continue;
                }

                await decodeGate.WaitAsync(cancellationToken);
                try
                {
                    BitmapSource? thumb = _tieredLoader.TryDecodeDownloaded(
                        fileName,
                        tempPath,
                        hints,
                        decodeMode);
                    if (thumb != null)
                    {
                        _logger.LogDebug(
                            "FTP preview decoded at {MaxBytes} bytes (tier {TierIndex}) for {RemotePath}.",
                            maxBytes,
                            tierIndex,
                            remotePath);
                        return thumb;
                    }
                }
                finally
                {
                    decodeGate.Release();
                }
            }

            return null;
        }

        private async Task<BitmapSource?> TryLoadSiblingPreviewAsync(
            FtpEndpoint endpoint,
            FtpThumbnailWorkItem workItem,
            string tempPath,
            ThumbnailHints? hints,
            bool useFluentFtp,
            SemaphoreSlim decodeGate,
            CancellationToken cancellationToken)
        {
            foreach (string siblingPath in GetRenderedSiblingRemotePaths(workItem.RemotePath, workItem.FileName))
            {
                BitmapSource? thumb = await TryTieredDownloadAndDecodeAsync(
                    endpoint,
                    siblingPath,
                    Path.GetFileName(siblingPath),
                    tempPath,
                    hints,
                    useFluentFtp,
                    decodeGate,
                    cancellationToken);
                if (thumb != null)
                {
                    return thumb;
                }
            }

            return null;
        }

        private static IEnumerable<string> GetRenderedSiblingRemotePaths(string remotePath, string fileName)
        {
            int slash = remotePath.LastIndexOf('/');
            string directory = slash >= 0 ? remotePath[..slash] : string.Empty;
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            foreach (string ext in new[] { ".heic", ".heif", ".jpg", ".jpeg" })
            {
                yield return string.IsNullOrEmpty(directory) ? "/" + baseName + ext : directory + "/" + baseName + ext;
            }
        }

        private static bool ShouldTryFullDownload(FtpThumbnailWorkItem workItem)
        {
            string ext = Path.GetExtension(workItem.FileName);
            if (MediaExtensions.IsVideoExtension(ext))
            {
                return false;
            }

            return workItem.FileSize <= 0 || workItem.FileSize <= 25 * 1024 * 1024;
        }

        private static int GetThumbnailPriority(FtpThumbnailWorkItem item)
        {
            string ext = Path.GetExtension(item.FileName).ToLowerInvariant();
            if (ext is ".heic" or ".heif" or ".jpg" or ".jpeg" or ".png")
            {
                return 0;
            }

            if (MediaExtensions.IsVideoExtension(ext))
            {
                return 2;
            }

            if (MediaExtensions.IsRawExtension(ext))
            {
                return 3;
            }

            return 1;
        }

        private static void TryDeleteTemp(string tempPath)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
                // Ignore temp cleanup failures.
            }
        }
    }
}
