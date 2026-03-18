using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    public class LocalScanner
    {
        public List<ImportItem> Scan(string drivePath)
        {
            var items = new List<ImportItem>();

            if (!Directory.Exists(drivePath))
            {
                return items;
            }

            // Get standard file handles (Recursively)
            var files = Directory.GetFiles(drivePath, "*.*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                FileInfo info = new FileInfo(file);
                items.Add(new ImportItem
                {
                    SourcePath = info.FullName,
                    FileName = info.Name,
                    FileSize = info.Length,
                    DateTaken = info.LastWriteTime, // Fallback; EXIF parser will improve this in Sprint 3
                    IsVideo = IsVideoFile(info.Extension),
                    FileType = info.Extension.TrimStart('.').ToUpper()
                });
            }

            return items;
        }

        private bool IsVideoFile(string ext)
        {
            ext = ext.ToLower();
            return ext == ".mp4" || ext == ".mov" || ext == ".avi" || ext == ".mkv";
        }
    }
}
