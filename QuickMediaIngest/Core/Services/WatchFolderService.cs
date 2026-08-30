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

        public bool IsWatching => _watcher != null && _watcher.EnableRaisingEvents;
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
                        EnableRaisingEvents = true
                    };

                    _watcher.Created += OnCreated;
                    _logger.LogInformation("Watch-folder service started for {Path}", directoryPath);
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
            lock (_lock)
            {
                if (_watcher != null)
                {
                    _watcher.EnableRaisingEvents = false;
                    _watcher.Created -= OnCreated;
                    _watcher.Dispose();
                    _watcher = null;
                }
            }
        }

        public void Dispose()
        {
            StopWatching();
        }
    }
}
