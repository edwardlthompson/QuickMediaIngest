#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Remembers which picker volumes the user ticked (<c>scan-sources.json</c>).</summary>
    public static class ScanSourceStore
    {
        public static FileDto Load(IAppPaths paths)
        {
            try
            {
                string file = PathFor(paths);
                if (!File.Exists(file))
                {
                    return new FileDto();
                }

                return JsonSerializer.Deserialize<FileDto>(File.ReadAllText(file)) ?? new FileDto();
            }
            catch
            {
                return new FileDto();
            }
        }

        public static void Save(IAppPaths paths, IEnumerable<VolumeChoice> volumes)
        {
            try
            {
                string file = PathFor(paths);
                string? dir = Path.GetDirectoryName(file);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                VolumeChoice[] list = volumes.ToArray();
                var dto = new FileDto
                {
                    SeenPaths = list.Select(v => v.Path).ToArray(),
                    SelectedPaths = list.Where(v => v.IsSelected).Select(v => v.Path).ToArray(),
                };
                File.WriteAllText(file, JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch
            {
                // best effort
            }
        }

        public static bool IsSelected(string path, string label, FileDto memory)
        {
            if (memory.SeenPaths.Any(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase)))
            {
                return memory.SelectedPaths.Any(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
            }

            return VolumeScanHint.ShouldSelectByDefault(path, label);
        }

        private static string PathFor(IAppPaths paths) => Path.Combine(paths.AppDataRoot, "scan-sources.json");

        public sealed class FileDto
        {
            public string[] SeenPaths { get; set; } = Array.Empty<string>();

            public string[] SelectedPaths { get; set; } = Array.Empty<string>();
        }
    }
}
