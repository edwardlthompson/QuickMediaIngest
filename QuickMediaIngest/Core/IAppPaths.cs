#nullable enable
using System;
using System.IO;

namespace QuickMediaIngest.Core
{
    /// <summary>AppData layout for both heads. L1 Windows uses <see cref="DefaultAppPaths"/>; L2 adds XDG.</summary>
    public interface IAppPaths
    {
        string AppDataRoot { get; }
    }

    /// <summary>SpecialFolder.ApplicationData / QuickMediaIngest (unchanged Windows AppData shape).</summary>
    public sealed class DefaultAppPaths : IAppPaths
    {
        public static DefaultAppPaths Instance { get; } = new();

        public string AppDataRoot => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QuickMediaIngest");
    }
}
