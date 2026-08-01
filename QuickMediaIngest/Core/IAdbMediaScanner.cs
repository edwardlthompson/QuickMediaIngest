#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    public interface IAdbMediaScanner
    {
        /// <summary>
        /// Lists media under the session media root + FTP-style folder.
        /// Returns null on failure (caller should FTP fallback).
        /// </summary>
        Task<List<ImportItem>?> ScanAsync(
            AdbTransferSession session,
            string ftpRemoteFolder,
            bool includeSubfolders,
            CancellationToken cancellationToken = default);
    }
}
