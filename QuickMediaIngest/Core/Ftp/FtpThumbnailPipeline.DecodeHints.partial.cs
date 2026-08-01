#nullable enable
using System.IO;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    internal sealed partial class FtpThumbnailPipeline
    {
        private static (FtpPreviewDecodeMode Mode, ThumbnailHints Hints) ResolveDecodeHints(
            string tempPath,
            long knownFileSize,
            long maxBytes,
            int tierIndex,
            int tierCount,
            ThumbnailHints? hints)
        {
            bool complete = false;
            try
            {
                if (File.Exists(tempPath))
                {
                    long length = new FileInfo(tempPath).Length;
                    if (knownFileSize > 0 && length >= knownFileSize)
                    {
                        complete = true;
                    }
                    else if (knownFileSize <= 0 && length > 0 && length < maxBytes)
                    {
                        // Cap not filled → remote EOF (full file within budget).
                        complete = true;
                    }
                }
            }
            catch
            {
                complete = false;
            }

            if (complete)
            {
                return (
                    FtpPreviewDecodeMode.CompleteFile,
                    new ThumbnailHints
                    {
                        DeferRawShellMilliseconds = hints?.DeferRawShellMilliseconds ?? 0,
                        IsPartialPreview = false,
                    });
            }

            FtpPreviewDecodeMode mode = tierIndex < tierCount - 1
                ? FtpPreviewDecodeMode.TieredPartial
                : FtpPreviewDecodeMode.TieredFinalCap;

            return (
                mode,
                new ThumbnailHints
                {
                    DeferRawShellMilliseconds = hints?.DeferRawShellMilliseconds ?? 0,
                    IsPartialPreview = true,
                });
        }
    }
}
