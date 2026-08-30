#nullable enable
using System.IO;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    /// <summary>Shared shoot-folder naming for import and post-import export.</summary>
    public static class GroupFolderNaming
    {
        public static string GetTargetFolderName(ItemGroup group) =>
            GetTargetFolderName(group, template: null);

        public static string GetTargetFolderName(
            ItemGroup group,
            string? template = null,
            string? job = null,
            string? client = null,
            string? camera = null)
        {
            string datePart = group.StartDate.ToString("yyyyMMdd_HHmmss");
            string safeTitle = string.Join(
                "_",
                group.Title.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
            if (string.IsNullOrWhiteSpace(safeTitle))
            {
                safeTitle = "Group";
            }

            if (string.IsNullOrWhiteSpace(template))
            {
                return $"{datePart}_{safeTitle}";
            }

            string result = template
                .Replace("[Date]", group.StartDate.ToString("yyyy-MM-dd"), StringComparison.Ordinal)
                .Replace("[YYYY]", group.StartDate.ToString("yyyy"), StringComparison.Ordinal)
                .Replace("[MM]", group.StartDate.ToString("MM"), StringComparison.Ordinal)
                .Replace("[DD]", group.StartDate.ToString("dd"), StringComparison.Ordinal)
                .Replace("[ShootTitle]", safeTitle, StringComparison.Ordinal)
                .Replace("[Job]", string.IsNullOrWhiteSpace(job) ? "Job" : job.Trim(), StringComparison.Ordinal)
                .Replace("[Client]", string.IsNullOrWhiteSpace(client) ? "Client" : client.Trim(), StringComparison.Ordinal)
                .Replace("[Camera]", string.IsNullOrWhiteSpace(camera) ? "Camera" : camera.Trim(), StringComparison.Ordinal);

            string safeFolder = string.Join(
                "_",
                result.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();

            return string.IsNullOrWhiteSpace(safeFolder) ? $"{datePart}_{safeTitle}" : safeFolder;
        }
    }
}
