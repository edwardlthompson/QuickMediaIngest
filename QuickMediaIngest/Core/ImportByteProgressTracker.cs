#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Thread-safe byte progress for parallel file copies.
    /// File counter may still advance in small bursts when parallel jobs finish together;
    /// EffectiveBytes includes in-flight partial copy progress for smooth bar/ETA.
    /// </summary>
    public sealed class ImportByteProgressTracker
    {
        private readonly object _lock = new();
        private readonly Dictionary<string, long> _inFlight = new(StringComparer.Ordinal);
        private long _completedBytes;
        private int _filesCompleted;
        private int _filesFailed;

        public ImportByteProgressTracker(long totalBytes, int totalFiles)
        {
            TotalBytes = Math.Max(0, totalBytes);
            TotalFiles = Math.Max(0, totalFiles);
        }

        public long TotalBytes { get; }
        public int TotalFiles { get; }

        public event Action<ImportByteProgressSnapshot>? ProgressChanged;

        public ImportByteProgressSnapshot GetSnapshot()
        {
            lock (_lock)
            {
                return BuildSnapshotLocked();
            }
        }

        public void RegisterFileStarted(string sourceKey, long fileSizeBytes)
        {
            if (string.IsNullOrWhiteSpace(sourceKey))
            {
                sourceKey = Guid.NewGuid().ToString("N");
            }

            lock (_lock)
            {
                _inFlight[sourceKey] = 0;
                PublishLocked();
            }
        }

        public void ReportBytes(string sourceKey, long bytesCopiedSoFar)
        {
            if (string.IsNullOrWhiteSpace(sourceKey))
            {
                return;
            }

            lock (_lock)
            {
                if (!_inFlight.ContainsKey(sourceKey))
                {
                    return;
                }

                _inFlight[sourceKey] = Math.Max(0, bytesCopiedSoFar);
                PublishLocked();
            }
        }

        public void RegisterFileCompleted(string sourceKey, long fileSizeBytes, bool success)
        {
            if (string.IsNullOrWhiteSpace(sourceKey))
            {
                sourceKey = Guid.NewGuid().ToString("N");
            }

            lock (_lock)
            {
                long credited = Math.Max(0, fileSizeBytes);
                if (_inFlight.TryGetValue(sourceKey, out long partial))
                {
                    credited = success ? Math.Max(partial, credited) : partial;
                    _inFlight.Remove(sourceKey);
                }

                _completedBytes += credited;
                if (success)
                {
                    _filesCompleted++;
                }
                else
                {
                    _filesFailed++;
                }

                PublishLocked();
            }
        }

        private ImportByteProgressSnapshot BuildSnapshotLocked()
        {
            long inFlightBytes = _inFlight.Values.Sum();
            return new ImportByteProgressSnapshot
            {
                TotalBytes = TotalBytes,
                TotalFiles = TotalFiles,
                CompletedBytes = _completedBytes,
                EffectiveBytes = _completedBytes + inFlightBytes,
                FilesCompleted = _filesCompleted,
                FilesFailed = _filesFailed,
                FilesInFlight = _inFlight.Count,
            };
        }

        private void PublishLocked()
        {
            ProgressChanged?.Invoke(BuildSnapshotLocked());
        }
    }
}
