using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentFTP;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    public class FtpScanner
    {
        public async Task<List<ImportItem>> ScanAsync(string host, int port, string remotePath)
        {
            var items = new List<ImportItem>();

            // WiFi FTP Server Android usually uses anonymous or user/pass
            using (var client = new AsyncFtpClient(host, "anonymous", "anonymous", port))
            {
                await client.Connect();

                // Get recursive listing or standard listing
                var ftpItems = await client.GetListing(remotePath, FtpListOption.Recursive);

                foreach (var ftpItem in ftpItems)
                {
                    if (ftpItem.Type == FtpObjectType.File)
                    {
                        items.Add(new ImportItem
                        {
                            SourcePath = ftpItem.FullName,
                            FileName = ftpItem.Name,
                            FileSize = ftpItem.Size,
                            DateTaken = ftpItem.Modified, // Fallback to mod date on FTP before EXIF extract
                            IsVideo = IsVideoFile(ftpItem.Name),
                            FileType = Path.GetExtension(ftpItem.Name).TrimStart('.').ToUpper()
                        });
                    }
                }
            }
            return items;
        }

        private bool IsVideoFile(string name)
        {
            string ext = Path.GetExtension(name).ToLower();
            return ext == ".mp4" || ext == ".mov" || ext == ".avi" || ext == ".mkv";
        }
    }
}
