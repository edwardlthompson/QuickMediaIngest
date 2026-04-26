#nullable enable
using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Reads metadata (such as EXIF) from media files and updates import items.
    /// </summary>
    public class MetadataReader : IMetadataReader
    {
        private readonly ILogger<MetadataReader> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataReader"/> class.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output.</param>
        public MetadataReader(ILogger<MetadataReader> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Reads metadata from the specified import item and updates its properties.
        /// </summary>
        /// <param name="item">The import item to update with metadata.</param>
        public void ReadMetadata(ImportItem item)
        {
            if (!File.Exists(item.SourcePath)) 
            {
                return; // Can only extract local file streams easily
            }

            try
            {
                var directories = ImageMetadataReader.ReadMetadata(item.SourcePath);
                var subIfdDir = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();

                if (subIfdDir != null && subIfdDir.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out DateTime dateTime))
                {
                    item.DateTaken = ApplyExifSubsecondPrecision(subIfdDir, dateTime);
                }
                else
                {
                    // Fallback ensures [fff] tokens are not always 000 when metadata lacks ms precision.
                    var fallback = File.GetLastWriteTime(item.SourcePath);
                    if (fallback.Year > 1900)
                    {
                        item.DateTaken = fallback;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read metadata for {FileName}.", item.FileName);
            }
        }

        private static DateTime ApplyExifSubsecondPrecision(ExifSubIfdDirectory subIfdDir, DateTime baseDateTime)
        {
            try
            {
                string? subsec = subIfdDir.GetDescription(ExifDirectoryBase.TagSubsecondTimeOriginal)
                    ?? subIfdDir.GetDescription(ExifDirectoryBase.TagSubsecondTime);
                if (string.IsNullOrWhiteSpace(subsec))
                {
                    return baseDateTime;
                }

                string digits = new string(subsec.Where(char.IsDigit).ToArray());
                if (digits.Length == 0)
                {
                    return baseDateTime;
                }

                if (digits.Length > 3)
                {
                    digits = digits.Substring(0, 3);
                }
                else if (digits.Length < 3)
                {
                    digits = digits.PadRight(3, '0');
                }

                if (!int.TryParse(digits, out int milliseconds))
                {
                    return baseDateTime;
                }

                return new DateTime(
                    baseDateTime.Year,
                    baseDateTime.Month,
                    baseDateTime.Day,
                    baseDateTime.Hour,
                    baseDateTime.Minute,
                    baseDateTime.Second,
                    milliseconds,
                    baseDateTime.Kind);
            }
            catch
            {
                return baseDateTime;
            }
        }
    }
}
