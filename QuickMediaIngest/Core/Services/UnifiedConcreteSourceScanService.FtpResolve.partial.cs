#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.Services
{
    public sealed partial class UnifiedConcreteSourceScanService
    {
        private async Task<List<ImportItem>> ResolveFtpOrAdbItemsAsync(
            QuickMediaIngest.FtpSourceItem ftp,
            bool scanSubfolders,
            bool preferAdbTransfer,
            ConcurrentBag<string> ftpListingFailures,
            CancellationToken cancellationToken)
        {
            if (preferAdbTransfer)
            {
                AdbTransferSession? session = AdbTransferEligibility.TryResolve(ftp.RemoteFolder, _adbPathProbe);
                if (session is { } adb)
                {
                    List<ImportItem>? adbItems = await _adbMediaScanner
                        .ScanAsync(adb, ftp.RemoteFolder, scanSubfolders, cancellationToken)
                        .ConfigureAwait(false);
                    if (adbItems != null)
                    {
                        return adbItems;
                    }

                    List<ImportItem> ftpItems = await ScanFtpAsync(
                            ftp.Host, ftp.Port, ftp.User, ftp.Pass, ftp.RemoteFolder, scanSubfolders, ftpListingFailures, cancellationToken)
                        .ConfigureAwait(false);
                    return await FtpAdbAliasFilter
                        .FilterAsync(ftpItems, adb, _adbPathProbe, cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            return await ScanFtpAsync(
                    ftp.Host, ftp.Port, ftp.User, ftp.Pass, ftp.RemoteFolder, scanSubfolders, ftpListingFailures, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<List<ImportItem>> ScanFtpAsync(
            string host,
            int port,
            string user,
            string pass,
            string remoteFolder,
            bool scanSubfolders,
            ConcurrentBag<string> ftpListingFailures,
            CancellationToken cancellationToken)
        {
            return await _ftpScanner.ScanAsync(
                host,
                port,
                user,
                pass,
                FtpPathNormalizer.Normalize(remoteFolder),
                scanSubfolders,
                UnifiedFtpScanBudgets.ListingSeconds,
                cancellationToken,
                progress =>
                {
                    if (!string.IsNullOrWhiteSpace(progress.Note) &&
                        progress.Note.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                    {
                        ftpListingFailures.Add($"{progress.CurrentFolder} - {progress.Note}");
                    }
                }).ConfigureAwait(false);
        }
    }
}
