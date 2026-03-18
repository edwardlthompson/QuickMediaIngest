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

        private readonly IFileProvider _provider;

        public IngestEngine(IFileProvider provider)
        {
            _provider = provider;
        }

        public async Task IngestGroupAsync(ItemGroup group, string destinationRoot, string namingTemplate, CancellationToken cancellationToken)
        {
            if (group == null || group.Items.Count == 0) return;

            string folderName = GetTargetFolderName(group);
            string targetDir = Path.Combine(destinationRoot, folderName);

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            int total = group.Items.Count;
            int current = 0;

            foreach (var item in group.Items)
            {
                if (cancellationToken.IsCancellationRequested) break;
                if (!item.IsSelected) continue;

                current++;
                string status = $"Copying {item.FileName} ({current}/{total})";
                ProgressChanged?.Invoke((current * 100) / total, status);

                string destFileName = ResolveFileName(item, targetDir, namingTemplate);
                string destPath = Path.Combine(targetDir, destFileName);

                try
                {
                    await _provider.CopyAsync(item.SourcePath, destPath, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Copy Error] {item.FileName}: {ex.Message}");
                }
            }

            ProgressChanged?.Invoke(100, "Ingest Completed!");
        }

        private string GetTargetFolderName(ItemGroup group)
        {
            string start = group.StartDate.ToString("yyyy-MM-dd");
            string end = group.EndDate.ToString("yyyy-MM-dd");
            string name = string.IsNullOrEmpty(group.Title) ? "Shoot" : group.Title;

            if (start == end)
            {
                return $"{start}+{name}";
            }
            return $"{start} to {end}+{name}";
        }

        private string ResolveFileName(ImportItem item, string targetDir, string template)
        {
            string ext = Path.GetExtension(item.FileName);
            string outputName = template;

            if (string.IsNullOrEmpty(outputName))
            {
                outputName = "[Date]_[Time]_[Original]"; // Default fallback
            }

            // Replace Tokens
            outputName = outputName.Replace("[Date]", item.DateTaken.ToString("yyyy-MM-dd"));
            outputName = outputName.Replace("[Time]", item.DateTaken.ToString("HH-mm-ss"));
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
    }
}
