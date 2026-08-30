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
}
