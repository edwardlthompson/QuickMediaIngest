using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    public class LocalScanner
    {
        public List<ImportItem> Scan(string sourcePath, bool includeSubfolders, Action<int, int>? folderProgressCallback = null)
        {
            var items = new List<ImportItem>();

            if (!Directory.Exists(sourcePath))
            {
                return items;
            }

            List<string> foldersToScan;
            try
            {
                foldersToScan = includeSubfolders
                    ? Directory.EnumerateDirectories(sourcePath, "*", SearchOption.AllDirectories).ToList()
                    : new List<string>();
            }
            catch
            {
                return items;
            }

            foldersToScan.Insert(0, sourcePath);
            int totalFolders = foldersToScan.Count;
            int scannedFolders = 0;

            foreach (var folder in foldersToScan)
            {
                IEnumerable<string> files;
                try
                {
                    files = Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly);
                }
                catch
                {
                    scannedFolders++;
                    folderProgressCallback?.Invoke(scannedFolders, totalFolders);
                    continue;
                }

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

                scannedFolders++;
                folderProgressCallback?.Invoke(scannedFolders, totalFolders);
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
