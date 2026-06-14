#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.Services
{
    public sealed class FtpThumbnailLoadOptions
    {
        public int DownloadParallelism { get; init; } = 3;
        public int DecodeParallelism { get; init; } = 4;
        public string PerformanceMode { get; init; } = "Balanced";
    }

    public sealed class FtpThumbnailWorkItem
    {
        public string ItemKey { get; init; } = string.Empty;
        public string RemotePath { get; init; } = string.Empty;
        public string FileName { get; init; } = string.Empty;
        public long FileSize { get; init; }
    }

    public sealed class FtpThumbnailItemResult
    {
        public string ItemKey { get; init; } = string.Empty;
        public BitmapSource? Thumbnail { get; init; }
        public ThumbnailPreviewStatus Status { get; init; }
    }

    public sealed class FtpThumbnailBatchResult
    {
        public int LoadedCount { get; init; }
        public int SkippedCount { get; init; }
        public IReadOnlyList<FtpThumbnailItemResult> Items { get; init; } = [];
    }

    public sealed class FtpThumbnailProgress
    {
        public int Processed { get; init; }
        public int Total { get; init; }
        public string? CurrentRemotePath { get; init; }
    }

    public interface IFtpThumbnailService
    {
        Task<FtpThumbnailBatchResult> LoadBatchAsync(
            FtpEndpoint endpoint,
            IReadOnlyList<FtpThumbnailWorkItem> items,
            ThumbnailHints? hints,
            FtpThumbnailLoadOptions options,
            Func<FtpThumbnailProgress, Task>? onProgress,
            Func<FtpThumbnailItemResult, Task>? onItemCompleted,
            CancellationToken cancellationToken);
    }
}
