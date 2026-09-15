#nullable enable

namespace QuickMediaIngest.Core
{
    /// <summary>Linux card watch: inotify (FileSystemWatcher) + 500ms debounce + 3s poll fallback.</summary>
    public static class PathWatchTimings
    {
        public const int DebounceMilliseconds = 500;
        public const int PollMilliseconds = 3000;
    }
}
