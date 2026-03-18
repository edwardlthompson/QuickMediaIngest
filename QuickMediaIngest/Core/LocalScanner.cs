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

            var files = Directory.GetFiles(drivePath, "*.*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                FileInfo info = new FileInfo(file);
                string ext = info.Extension.ToLower();

                // Skip non-media metadata files (CTG, DAT, etc.)
                if (!IsMediaFile(ext)) continue;

                items.Add(new ImportItem
                {
                    SourcePath = info.FullName,
                    FileName = info.Name,
                    FileSize = info.Length,
                    DateTaken = info.LastWriteTime, 
                    IsVideo = IsVideoFile(ext),
                    FileType = ext.TrimStart('.').ToUpper()
                });
            }

            return items;
        }

        private bool IsMediaFile(string ext)
        {
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || 
                   ext == ".cr2" || ext == ".cr3" || ext == ".nef" || ext == ".arw" || 
                   IsVideoFile(ext);
        }

        private bool IsVideoFile(string ext)
        {
            return ext == ".mp4" || ext == ".mov" || ext == ".avi" || ext == ".mkv";
        }
    }
}
