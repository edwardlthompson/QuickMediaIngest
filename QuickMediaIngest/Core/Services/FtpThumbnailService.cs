#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Core.Services
{
    public sealed class FtpThumbnailService : IFtpThumbnailService
    {
        private readonly FtpFileDownloader _fileDownloader;
        private readonly IThumbnailService _thumbnailService;
        private readonly ILogger<FtpThumbnailService> _logger;

        public FtpThumbnailService(
            IThumbnailService thumbnailService,
            FtpFileDownloader fileDownloader,
            ILogger<FtpThumbnailService> logger)
        {
            _thumbnailService = thumbnailService;
            _fileDownloader = fileDownloader;
            _logger = logger;
        }

        public async Task<FtpThumbnailBatchResult> LoadBatchAsync(
            FtpEndpoint endpoint,
            IReadOnlyList<FtpThumbnailWorkItem> items,
            ThumbnailHints? hints,
            FtpThumbnailLoadOptions options,
            Func<FtpThumbnailProgress, Task>? onProgress,
            Func<FtpThumbnailItemResult, Task>? onItemCompleted,
            CancellationToken cancellationToken)
        {
            if (items.Count == 0)
            {
                return new FtpThumbnailBatchResult();
            }

            if (string.IsNullOrEmpty(endpoint.Pass))
            {
                _logger.LogWarning(
                    "FTP thumbnail batch skipped: empty password for {Host}:{Port}.",
                    endpoint.Host,
                    endpoint.Port);
                return new FtpThumbnailBatchResult
                {
                    SkippedCount = items.Count,
                    Items = items.Select(i => new FtpThumbnailItemResult
                    {
                        ItemKey = i.ItemKey,
                        Status = ThumbnailPreviewStatus.Failed
                    }).ToList()
                };
            }

            var pipeline = new FtpThumbnailPipeline(_fileDownloader, _thumbnailService, _logger);
            FtpThumbnailBatchResult result = await pipeline.RunAsync(
                endpoint,
                items,
                hints,
                options,
                onProgress,
                onItemCompleted,
                cancellationToken);

            _logger.LogInformation(
                "FTP thumbnail batch finished for {Host}:{Port}: loaded {Loaded}, skipped {Skipped}, total {Total}.",
                endpoint.Host,
                endpoint.Port,
                result.LoadedCount,
                result.SkippedCount,
                items.Count);

            return result;
        }
    }
}
