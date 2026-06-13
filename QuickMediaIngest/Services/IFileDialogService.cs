#nullable enable
namespace QuickMediaIngest.Services
{
    public interface IFileDialogService
    {
        bool TryPickFolder(string? initialDirectory, string title, out string selectedFolder);
    }
}
