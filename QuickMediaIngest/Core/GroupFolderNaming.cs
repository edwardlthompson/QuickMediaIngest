#nullable enable
using System.IO;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    /// <summary>Shared shoot-folder naming for import and post-import export.</summary>
    public static class GroupFolderNaming
    {
        public static string GetTargetFolderName(ItemGroup group)
        {
            string datePart = group.StartDate.ToString("yyyyMMdd_HHmmss");
            string safeTitle = string.Join(
                "_",
                group.Title.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
            if (string.IsNullOrWhiteSpace(safeTitle))
            {
                safeTitle = "Group";
            }

            return $"{datePart}_{safeTitle}";
        }
    }
}
