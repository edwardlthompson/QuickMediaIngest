#nullable enable
namespace QuickMediaIngest.Services
{
    public interface IShellService
    {
        void OpenFolder(string path);
        void OpenUrl(string url);
    }
}
