using System;
using System.IO;
using System.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    public class MetadataReader
    {
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
                Console.WriteLine($"[Metadata Error] {item.FileName}: {ex.Message}");
            }
        }
    }
}
