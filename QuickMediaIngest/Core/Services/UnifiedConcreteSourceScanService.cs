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

namespace QuickMediaIngest.Core.Services
{
    public sealed class UnifiedConcreteSourceScanService : IUnifiedConcreteSourceScanService
    {
        private readonly ILocalScanner _scanner;
        private readonly IFtpScanner _ftpScanner;
        private readonly ILogger<UnifiedConcreteSourceScanService> _logger;

        public UnifiedConcreteSourceScanService(
            ILocalScanner scanner,
            IFtpScanner ftpScanner,
            ILogger<UnifiedConcreteSourceScanService> logger)
        {
            _scanner = scanner;
            _ftpScanner = ftpScanner;
            _logger = logger;
        }

        public async Task<UnifiedScanMergeResult> MergeAllAsync(
            IReadOnlyList<object> concreteSources,
            bool forceRefresh,
            bool scanSubfolders,
            IDictionary<string, List<ImportItem>> itemCache,
            IProgress<(int Completed, int Total)>? mergeProgress = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Unified merge started for {SourceCount} sources (forceRefresh={ForceRefresh}).", concreteSources.Count, forceRefresh);
            var ftpListingFailures = new ConcurrentBag<string>();
            var cacheSync = new object();
            int completed = 0;
            int totalSources = concreteSources.Count;

            async Task<List<ImportItem>> ScanOneAsync(object src)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (src is string drive)
                {
                    string localPath = drive;
                    string localKey = FtpPathNormalizer.BuildLocalSourceKey(localPath);

                    lock (cacheSync)
                    {
                        if (!forceRefresh && itemCache.TryGetValue(localKey, out var cachedLocal))
                        {
                            return ImportItemListHelper.CloneItems(cachedLocal);
                        }
                    }

                    List<ImportItem> sourceItems;
                    if (!Directory.Exists(localPath))
                    {
                        sourceItems = new List<ImportItem>();
                    }
                    else
                    {
                        sourceItems = await Task.Run(() => _scanner.Scan(localPath, scanSubfolders), cancellationToken).ConfigureAwait(false);
                    }

                    ImportItemListHelper.StampItems(sourceItems, localKey, false);
                    lock (cacheSync)
                    {
                        itemCache[localKey] = ImportItemListHelper.CloneItems(sourceItems);
                    }

                    return sourceItems;
                }

                if (src is QuickMediaIngest.FtpSourceItem ftp)
                {
                    string host = ftp.Host;
                    int port = ftp.Port;
                    string user = ftp.User;
                    string pass = ftp.Pass;
                    string remoteFolder = ftp.RemoteFolder;
                    string ftpKey = FtpPathNormalizer.BuildFtpSourceKey(host, port, remoteFolder);

                    lock (cacheSync)
                    {
                        if (!forceRefresh && itemCache.TryGetValue(ftpKey, out var cachedFtp))
                        {
                            return ImportItemListHelper.CloneItems(cachedFtp);
                        }
                    }

                    List<ImportItem> sourceItems = await _ftpScanner.ScanAsync(
                        host,
                        port,
                        user,
                        pass,
                        FtpPathNormalizer.Normalize(remoteFolder),
                        scanSubfolders,
                        120,
                        cancellationToken,
                        progress =>
                        {
                            if (!string.IsNullOrWhiteSpace(progress.Note) &&
                                progress.Note.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                            {
                                ftpListingFailures.Add($"{progress.CurrentFolder} - {progress.Note}");
                            }
                        }).ConfigureAwait(false);

                    ImportItemListHelper.StampItems(sourceItems, ftpKey, true);
                    lock (cacheSync)
                    {
                        itemCache[ftpKey] = ImportItemListHelper.CloneItems(sourceItems);
                    }

                    return sourceItems;
                }

                return new List<ImportItem>();
            }

            async Task<List<ImportItem>> ScanOneWithProgressAsync(object src)
            {
                List<ImportItem> result = await ScanOneAsync(src).ConfigureAwait(false);
                int done = Interlocked.Increment(ref completed);
                mergeProgress?.Report((done, Math.Max(totalSources, 1)));
                return result;
            }

            List<ImportItem>[] parallelResults =
                await Task.WhenAll(concreteSources.Select(src => ScanOneWithProgressAsync(src))).ConfigureAwait(false);

            var unifiedItems = parallelResults.SelectMany(r => r).ToList();

            return new UnifiedScanMergeResult
            {
                UnifiedItems = unifiedItems,
                FtpListingFailures = new HashSet<string>(ftpListingFailures, StringComparer.OrdinalIgnoreCase)
            };
        }
    }
}
