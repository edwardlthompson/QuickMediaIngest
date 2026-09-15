#nullable enable
using System.IO;
using System.Text.Json;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Persists first-run dismiss under AppData (Linux XDG or Windows ApplicationData).</summary>
    public sealed class OnboardingStore
    {
        private readonly string _path;

        public OnboardingStore(IAppPaths paths)
        {
            _path = Path.Combine(paths.AppDataRoot, "onboarding.json");
        }

        public bool IsDismissed()
        {
            try
            {
                if (!File.Exists(_path))
                {
                    return false;
                }

                OnboardingFile? file = JsonSerializer.Deserialize<OnboardingFile>(File.ReadAllText(_path));
                return file?.Dismissed == true;
            }
            catch
            {
                return false;
            }
        }

        public void SaveDismissed()
        {
            try
            {
                string? dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(_path, JsonSerializer.Serialize(new OnboardingFile { Dismissed = true }));
            }
            catch
            {
                // Best effort — next launch may show Welcome again.
            }
        }

        private sealed class OnboardingFile
        {
            public bool Dismissed { get; set; }
        }
    }
}
