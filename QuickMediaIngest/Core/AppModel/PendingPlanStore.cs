#nullable enable
using System;
using System.IO;
using System.Text.Json;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Persists <c>pending-import.json</c> for resume after a crash or pause.</summary>
    public sealed class PendingPlanStore
    {
        private readonly string _path;

        public PendingPlanStore(IAppPaths paths)
        {
            _path = Path.Combine(paths.AppDataRoot, "pending-import.json");
        }

        public PendingImportPlan? Load()
        {
            try
            {
                if (!File.Exists(_path))
                {
                    return null;
                }

                return JsonSerializer.Deserialize<PendingImportPlan>(File.ReadAllText(_path));
            }
            catch
            {
                return null;
            }
        }

        public void Save(PendingImportPlan plan)
        {
            try
            {
                string? dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(_path, JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));
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
