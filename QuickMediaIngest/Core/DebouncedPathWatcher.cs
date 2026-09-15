#nullable enable
using System;
using System.IO;
using System.Threading;

namespace QuickMediaIngest.Core
{
    /// <summary>Debounced directory watch; Linux FileSystemWatcher is inotify. Poll timer covers missed events.</summary>
    public sealed class DebouncedPathWatcher : IDisposable
    {
        private readonly FileSystemWatcher? _watcher;
        private readonly Timer _poll;
        private readonly Timer _debounce;
        private readonly Action _onChanged;
        private int _pending;

        public DebouncedPathWatcher(string directory, Action onChanged)
        {
            _onChanged = onChanged;
            _debounce = new Timer(_ =>
            {
                if (Interlocked.Exchange(ref _pending, 0) == 1)
                {
                    _onChanged();
                }
            });
            _poll = new Timer(_ => Request(), null, PathWatchTimings.PollMilliseconds, PathWatchTimings.PollMilliseconds);
            if (Directory.Exists(directory))
            {
                FileSystemWatcher? watcher = null;
                try
                {
                    watcher = new FileSystemWatcher(directory)
                    {
                        IncludeSubdirectories = false,
                    };
                    watcher.Created += (_, _) => Request();
                    watcher.Deleted += (_, _) => Request();
                    watcher.Changed += (_, _) => Request();
                    watcher.Renamed += (_, _) => Request();
                    watcher.EnableRaisingEvents = true;
                    _watcher = watcher;
                }
                catch (IOException)
                {
                    watcher?.Dispose();
                }
            }
        }

        private void Request()
        {
            Interlocked.Exchange(ref _pending, 1);
            _debounce.Change(PathWatchTimings.DebounceMilliseconds, Timeout.Infinite);
        }

        public void Dispose()
        {
            try
            {
                if (_watcher != null)
                {
                    _watcher.EnableRaisingEvents = false;
                }
            }
            catch
            {
                // inotify/ReadDirectoryChanges can already be torn down
            }

            _poll.Dispose();
            _debounce.Dispose();
            try
            {
                _watcher?.Dispose();
            }
            catch
            {
                // Windows FileSystemWatcher.Dispose can throw after EnableRaisingEvents=false
            }
        }
    }
}
