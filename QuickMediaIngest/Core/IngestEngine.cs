using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    public class IngestEngine
    {
        // Event for UI progress monitoring (Percent, Status Message)
        public event Action<int, string>? ProgressChanged;
        public event Action<IngestProgressInfo>? ItemProcessed;

        private readonly IFileProvider _provider;

        public IngestEngine(IFileProvider provider)
        {
            _provider = provider;
        }

        public async Task IngestGroupAsync(ItemGroup group, string destinationRoot, string namingTemplate, CancellationToken cancellationToken)
        {
            if (group == null || group.Items.Count == 0) return;

            // Check if there are any selected items; skip group if none are selected
            var selectedItems = group.Items.Where(i => i.IsSelected).ToList();
            if (selectedItems.Count == 0) return;

            string folderName = GetTargetFolderName(group);
            string targetDir = Path.Combine(destinationRoot, folderName);

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            int total = selectedItems.Count;
            int current = 0;

            foreach (var item in selectedItems)
            {
                if (cancellationToken.IsCancellationRequested) break;

                current++;
                string status = $"Copying {item.FileName} ({current}/{total})";
                ProgressChanged?.Invoke((current * 100) / total, status);

                string destFileName = ResolveFileName(item, targetDir, namingTemplate, group.Title);
                string destPath = Path.Combine(targetDir, destFileName);
                bool success = false;
                string errorMessage = string.Empty;

                try
                {
                    await _provider.CopyAsync(item.SourcePath, destPath, cancellationToken);
                    success = true;
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                    Console.WriteLine($"[Copy Error] {item.FileName}: {ex.Message}");
                }

                ItemProcessed?.Invoke(new IngestProgressInfo
                {
                    GroupTitle = string.IsNullOrWhiteSpace(group.Title) ? targetDir : group.Title,
                    GroupCurrent = current,
                    GroupTotal = total,
                    FileName = item.FileName,
                    Success = success,
                    ErrorMessage = errorMessage
                });
            }

            ProgressChanged?.Invoke(100, "Ingest Completed!");
        }

        private string GetTargetFolderName(ItemGroup group)
        {
            string start = group.StartDate.ToString("yyyy-MM-dd");
            string end = group.EndDate.ToString("yyyy-MM-dd");
            string name = group.Title;

            if (string.IsNullOrEmpty(name))
            {
                return start == end ? start : $"{start} to {end}";
            }

            return start == end ? $"{start}+{name}" : $"{start} to {end}+{name}";
        }

        private string ResolveFileName(ImportItem item, string targetDir, string template, string shootName)
        {
            string ext = Path.GetExtension(item.FileName);
            string outputName = template;
            string safeShootName = SanitizeFileNamePart(string.IsNullOrWhiteSpace(shootName) ? "Shoot" : shootName);

            if (string.IsNullOrEmpty(outputName))
            {
                outputName = "[Date]_[Time]_[Original]"; // Default fallback
            }

                        // Replace Tokens
            outputName = outputName.Replace("[Date]", item.DateTaken.ToString("yyyy-MM-dd"));
            outputName = outputName.Replace("[Time]", item.DateTaken.ToString("HH-mm-ss"));
            outputName = outputName.Replace("[YYYY]", item.DateTaken.ToString("yyyy"));
            outputName = outputName.Replace("[MM]", item.DateTaken.ToString("MM"));
            outputName = outputName.Replace("[DD]", item.DateTaken.ToString("dd"));
            outputName = outputName.Replace("[HH]", item.DateTaken.ToString("HH"));
            outputName = outputName.Replace("[mm]", item.DateTaken.ToString("mm"));
            outputName = outputName.Replace("[ss]", item.DateTaken.ToString("ss"));
            outputName = outputName.Replace("[ShootName]", safeShootName);
            outputName = outputName.Replace("[Original]", Path.GetFileNameWithoutExtension(item.FileName));
            outputName = outputName.Replace("[Ext]", ext.TrimStart('.'));

            string destFileName = $"{outputName}{ext}";
            string fullPath = Path.Combine(targetDir, destFileName);

            int counter = 1;
            while (File.Exists(fullPath))
            {
                destFileName = $"{outputName}_{counter:D2}{ext}";
                fullPath = Path.Combine(targetDir, destFileName);
                counter++;
            }

            return destFileName;
        }

        private static string SanitizeFileNamePart(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return value.Trim();
        }
    }

    public class IngestProgressInfo
    {
        public string GroupTitle { get; set; } = string.Empty;
        public int GroupCurrent { get; set; }
        public int GroupTotal { get; set; }
        public string FileName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
