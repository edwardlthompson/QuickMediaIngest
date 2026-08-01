#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.Services
{
    public sealed partial class UnifiedConcreteSourceScanService
    {
        private async Task<(List<ImportItem> Items, string SourceKey, bool IsFtp, string? FailureNote)> ScanOneDetailedAsync(
            object src,
            bool forceRefresh,
            bool scanSubfolders,
            bool preferAdbTransfer,
            IDictionary<string, List<ImportItem>> itemCache,
            object cacheSync,
            ConcurrentBag<string> ftpListingFailures,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (src is string drive)
            {
                List<ImportItem> local = await ScanLocalAsync(
                        drive, forceRefresh, scanSubfolders, itemCache, cacheSync, cancellationToken)
                    .ConfigureAwait(false);
                return (local, FtpPathNormalizer.BuildLocalSourceKey(drive), false, null);
            }

            if (src is QuickMediaIngest.FtpSourceItem ftp)
            {
                return await ScanFtpSourceAsync(
                        ftp, forceRefresh, scanSubfolders, preferAdbTransfer, itemCache, cacheSync, ftpListingFailures, cancellationToken)
                    .ConfigureAwait(false);
            }

            return (new List<ImportItem>(), "unknown", false, null);
        }

        private async Task<List<ImportItem>> ScanLocalAsync(
            string localPath,
            bool forceRefresh,
            bool scanSubfolders,
            IDictionary<string, List<ImportItem>> itemCache,
            object cacheSync,
            CancellationToken cancellationToken)
        {
            string localKey = FtpPathNormalizer.BuildLocalSourceKey(localPath);
            lock (cacheSync)
            {
                if (!forceRefresh && itemCache.TryGetValue(localKey, out var cachedLocal))
                {
                    _logger.LogInformation("Unified cache hit for {SourceKey}.", localKey);
                    return ImportItemListHelper.CloneItems(cachedLocal);
                }
            }

            _logger.LogInformation("Unified cache miss for {SourceKey}.", localKey);
            List<ImportItem> sourceItems = !Directory.Exists(localPath)
                ? new List<ImportItem>()
                : await Task.Run(() => _scanner.Scan(localPath, scanSubfolders), cancellationToken).ConfigureAwait(false);

            ImportItemListHelper.StampItems(sourceItems, localKey, false);
            lock (cacheSync)
            {
                itemCache[localKey] = ImportItemListHelper.CloneItems(sourceItems);
            }

            return sourceItems;
        }

        private async Task<(List<ImportItem> Items, string SourceKey, bool IsFtp, string? FailureNote)> ScanFtpSourceAsync(
            QuickMediaIngest.FtpSourceItem ftp,
            bool forceRefresh,
            bool scanSubfolders,
            bool preferAdbTransfer,
            IDictionary<string, List<ImportItem>> itemCache,
            object cacheSync,
            ConcurrentBag<string> ftpListingFailures,
            CancellationToken cancellationToken)
        {
            string ftpKey = FtpPathNormalizer.BuildFtpSourceKey(ftp.Host, ftp.Port, ftp.RemoteFolder);
            try
            {
                if (!forceRefresh &&
                    FtpSourceCooldown.IsCoolingDown(ftp.Host, ftp.Port, out TimeSpan remaining))
                {
                    string note = $"FTP {ftp.Host}:{ftp.Port} cooling down ({(int)remaining.TotalSeconds}s left)";
                    ftpListingFailures.Add(note);
                    List<ImportItem>? cached = TryGetCached(ftpKey, itemCache, cacheSync);
                    return (cached ?? new List<ImportItem>(), ftpKey, true, note);
                }

                lock (cacheSync)
                {
                    if (!forceRefresh && itemCache.TryGetValue(ftpKey, out var cachedFtp))
                    {
                        _logger.LogInformation("Unified cache hit for {SourceKey}.", ftpKey);
                        return (ImportItemListHelper.CloneItems(cachedFtp), ftpKey, true, null);
                    }
                }

                _logger.LogInformation("Unified cache miss for {SourceKey}.", ftpKey);

                // PreferAdb can succeed without FTP probe.
                if (!preferAdbTransfer || AdbTransferEligibility.TryResolve(ftp.RemoteFolder, _adbPathProbe) is null)
                {
                    (bool ok, string message) = await _ftpScanner.TestConnectionAsync(
                            ftp.Host,
                            ftp.Port,
                            ftp.User,
                            ftp.Pass,
                            FtpPathNormalizer.Normalize(ftp.RemoteFolder),
                            UnifiedFtpScanBudgets.ConnectProbeSeconds,
                            cancellationToken)
                        .ConfigureAwait(false);
                    if (!ok)
                    {
                        return SoftFailFtp(ftp, ftpKey, forceRefresh, itemCache, cacheSync, ftpListingFailures,
                            $"FTP connect failed {ftp.Host}:{ftp.Port} — {message}");
                    }
                }

                List<ImportItem> sourceItems = await ResolveFtpOrAdbItemsAsync(
                        ftp, scanSubfolders, preferAdbTransfer, ftpListingFailures, cancellationToken)
                    .ConfigureAwait(false);

                ImportItemListHelper.StampItems(sourceItems, ftpKey, true);
                lock (cacheSync)
                {
                    itemCache[ftpKey] = ImportItemListHelper.CloneItems(sourceItems);
                }

                return (sourceItems, ftpKey, true, null);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unified FTP soft-fail for {SourceKey}.", ftpKey);
                return SoftFailFtp(ftp, ftpKey, forceRefresh, itemCache, cacheSync, ftpListingFailures,
                    $"FTP scan failed {ftp.Host}:{ftp.Port} — {ex.Message}");
            }
        }

        private (List<ImportItem> Items, string SourceKey, bool IsFtp, string? FailureNote) SoftFailFtp(
            QuickMediaIngest.FtpSourceItem ftp,
            string ftpKey,
            bool forceRefresh,
            IDictionary<string, List<ImportItem>> itemCache,
            object cacheSync,
            ConcurrentBag<string> ftpListingFailures,
            string note)
        {
            FtpSourceCooldown.MarkFailed(ftp.Host, ftp.Port);
            ftpListingFailures.Add(note);
            if (!forceRefresh)
            {
                List<ImportItem>? cached = TryGetCached(ftpKey, itemCache, cacheSync);
                if (cached != null)
                {
                    return (cached, ftpKey, true, note);
                }
            }

            // Do not write empty into itemCache (no poison).
            return (new List<ImportItem>(), ftpKey, true, note);
        }

        private static List<ImportItem>? TryGetCached(
            string key,
            IDictionary<string, List<ImportItem>> itemCache,
            object cacheSync)
        {
            lock (cacheSync)
            {
                return itemCache.TryGetValue(key, out var cached)
                    ? ImportItemListHelper.CloneItems(cached)
                    : null;
            }
        }
    }
}
