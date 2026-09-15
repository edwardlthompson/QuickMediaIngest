#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>JSON list of import sessions under AppData (<c>import-history.json</c>).</summary>
    public sealed class ImportHistoryStore
    {
        public const int Cap = 50;

        private readonly string _path;

        public ImportHistoryStore(IAppPaths paths)
        {
            _path = Path.Combine(paths.AppDataRoot, "import-history.json");
        }

        public IReadOnlyList<ImportHistoryRecord> Load()
        {
            try
            {
                if (!File.Exists(_path))
                {
                    return Array.Empty<ImportHistoryRecord>();
                }

                return JsonSerializer.Deserialize<List<ImportHistoryRecord>>(File.ReadAllText(_path))
                    ?? new List<ImportHistoryRecord>();
            }
            catch
            {
                return Array.Empty<ImportHistoryRecord>();
            }
        }

        public void Save(IReadOnlyList<ImportHistoryRecord> records)
        {
            try
            {
                string? dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var capped = new List<ImportHistoryRecord>(records.Count > Cap ? Cap : records.Count);
                for (int i = 0; i < records.Count && capped.Count < Cap; i++)
                {
                    capped.Add(records[i]);
                }

                File.WriteAllText(_path, JsonSerializer.Serialize(capped));
            }
            catch
            {
                // Best effort.
            }
        }

        public void Clear()
        {
            try
            {
                if (File.Exists(_path))
                {
                    File.Delete(_path);
                }
            }
            catch
            {
                // Best effort.
            }
        }
    }
}
