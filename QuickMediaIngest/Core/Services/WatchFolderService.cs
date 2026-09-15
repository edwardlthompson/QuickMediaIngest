#nullable enable
using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core.Services
{
    public interface IWatchFolderService : IDisposable
    {
        bool IsWatching { get; }
        string? WatchedDirectory { get; }
        event EventHandler<string>? FileDetected;
        void StartWatching(string directoryPath);
        void StopWatching();
    }

    public sealed class WatchFolderService : IWatchFolderService
    {
        private readonly ILogger<WatchFolderService> _logger;
        private FileSystemWatcher? _watcher;
        private readonly object _lock = new();

        public bool IsWatching => _watcher != null;
        public string? WatchedDirectory => _watcher?.Path;
        public event EventHandler<string>? FileDetected;

        public WatchFolderService(ILogger<WatchFolderService> logger)
        {
            _logger = logger;
        }

        public void StartWatching(string directoryPath)
        {
            lock (_lock)
            {
                StopWatching();

                if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
                {
                    return;
                }

                try
                {
                    _watcher = new FileSystemWatcher(directoryPath)
                    {
                        IncludeSubdirectories = true,
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                    };
                    _watcher.Created += OnCreated;
                    try
                    {
                        _watcher.EnableRaisingEvents = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "inotify enable failed for {Path}; watcher still attached", directoryPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to start watch-folder on {Path}", directoryPath);
                }
            }
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            if (File.Exists(e.FullPath))
            {
                FileDetected?.Invoke(this, e.FullPath);
            }
        }

        public void StopWatching()
        {
            FileSystemWatcher? watcher;
            lock (_lock)
            {
                watcher = _watcher;
                _watcher = null;
            }

            if (watcher == null)
            {
                return;
            }

            try
            {
                watcher.EnableRaisingEvents = false;
            }
            catch
            {
                // already disabled
            }

            watcher.Created -= OnCreated;
            try
            {
                watcher.Dispose();
            }
            catch
            {
                // Windows runner can throw from FileSystemWatcher.Dispose
            }
        }

        public void Dispose()
        {
            StopWatching();
        }
    }
}
