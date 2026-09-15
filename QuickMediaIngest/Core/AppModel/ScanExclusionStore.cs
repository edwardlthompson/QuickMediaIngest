#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Persisted folder prefixes hidden from ingest-bench scans.</summary>
    public sealed class ScanExclusionStore
    {
        private readonly string _path;

        public ScanExclusionStore(IAppPaths paths)
        {
            _path = Path.Combine(paths.AppDataRoot, "scan-exclusions.json");
        }

        public IReadOnlyList<string> Load()
        {
            try
            {
                if (!File.Exists(_path))
                {
                    return Array.Empty<string>();
                }

                FileDto? dto = JsonSerializer.Deserialize<FileDto>(File.ReadAllText(_path));
                return dto?.Folders?.Where(f => !string.IsNullOrWhiteSpace(f)).Select(ScanExclusionMatcher.Normalize).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                    ?? new List<string>();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public void Save(IEnumerable<string> folders)
        {
            try
            {
                string? dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var dto = new FileDto
                {
                    Folders = folders
                        .Where(f => !string.IsNullOrWhiteSpace(f))
                        .Select(ScanExclusionMatcher.Normalize)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList(),
                };
                File.WriteAllText(_path, JsonSerializer.Serialize(dto));
            }
            catch
            {
                // Best effort.
            }
        }

        private sealed class FileDto
        {
            public List<string> Folders { get; set; } = new();
        }
    }
}
