#nullable enable
namespace QuickMediaIngest.Core.ImportUi
{
    /// <summary>Hedge-style afterglow label: destination leaf folder, WPF-free for both heads.</summary>
    public static class ImportAfterglow
    {
        public static string FolderLabel(string? destinationRoot)
        {
            if (string.IsNullOrWhiteSpace(destinationRoot))
            {
                return string.Empty;
            }

            string trimmed = destinationRoot.TrimEnd('/', '\\');
            int slash = trimmed.LastIndexOfAny(new[] { '/', '\\' });
            string name = slash >= 0 ? trimmed[(slash + 1)..] : trimmed;
            return string.IsNullOrEmpty(name) ? trimmed : name;
        }
    }
}
