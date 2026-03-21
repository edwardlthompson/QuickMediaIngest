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
                    item.DateTaken = dateTime;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read metadata for {FileName}.", item.FileName);
            }
        }
    }
}
