#nullable enable
using System.IO;
using System.Text.Json;

namespace QuickMediaIngest.Core.CrashCapture;

/// <summary>At-most-one pending crash file under AppData. Write failure returns false.</summary>
public sealed class FilePendingCrashStore : IPendingCrashStore
{
    private readonly string _path;
    private readonly object _gate = new();

    public FilePendingCrashStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QuickMediaIngest",
            "pending-crash.json");
    }

    public PendingCrash? Load()
    {
        lock (_gate)
        {
            try
            {
                if (!File.Exists(_path))
                {
                    return null;
                }

                return JsonSerializer.Deserialize<PendingCrash>(File.ReadAllText(_path));
            }
            catch
            {
                return null;
            }
        }
    }

    public bool Replace(PendingCrash record)
    {
        lock (_gate)
        {
            try
            {
                string? dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(_path, JsonSerializer.Serialize(record));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public void Clear()
    {
        lock (_gate)
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
                // Spec: write/delete failure drops the record.
            }
        }
    }

    private string GetDiscardedPath()
    {
        string? dir = Path.GetDirectoryName(_path);
        return Path.Combine(string.IsNullOrEmpty(dir) ? "." : dir, "discarded-crashes.json");
    }

    public void MarkDiscarded(string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(fingerprint)) return;
        lock (_gate)
        {
            try
            {
                Clear();
                string discPath = GetDiscardedPath();
                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (File.Exists(discPath))
                {
                    var existing = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(discPath));
                    if (existing != null) foreach (var e in existing) set.Add(e);
                }
                set.Add(fingerprint);
                File.WriteAllText(discPath, JsonSerializer.Serialize(set.ToList()));
            }
            catch
            {
                // Best effort
            }
        }
    }

    public bool IsDiscarded(string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(fingerprint)) return false;
        lock (_gate)
        {
            try
            {
                string discPath = GetDiscardedPath();
                if (!File.Exists(discPath)) return false;
                var list = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(discPath));
                return list?.Contains(fingerprint, StringComparer.OrdinalIgnoreCase) ?? false;
            }
            catch
            {
                return false;
            }
        }
    }
}
