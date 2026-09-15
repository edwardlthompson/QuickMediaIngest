#nullable enable
using QuickMediaIngest.Core;

namespace QuickMediaIngest.Windows
{
    /// <summary>Windows AppData adapter (same folder as today). Linux L2 replaces this with XDG.</summary>
    public sealed class WindowsAppPaths : IAppPaths
    {
        public string AppDataRoot => DefaultAppPaths.Instance.AppDataRoot;
    }
}
