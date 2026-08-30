#nullable enable
using System.Text.Json;
using QuickMediaIngest.Core.CrashCapture;
using QuickMediaIngest.Core.PrivacyReport;

namespace QuickMediaIngest
{
    public partial class App
    {
        private static void CaptureGoldenPathCrash(Exception? exception)
        {
            try
            {
                bool optIn = ReadSaveCrashDetailsPref();
                var service = new CrashCaptureService(new FilePendingCrashStore(), () => optIn);
                service.TryCapture(exception, typeof(App).Assembly.GetName().Version?.ToString(3));
            }
            catch
            {
                // Handler errors must not re-enter or take down the process.
            }
        }

        private static bool ReadSaveCrashDetailsPref()
        {
            try
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "QuickMediaIngest",
                    "config.json");
                if (!File.Exists(path))
                {
                    return false;
                }

                AppConfig? config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path));
                return config?.SaveCrashDetails == true;
            }
            catch
            {
                return false;
            }
        }

        private static string SanitizeCrashDump(string? text) =>
            PrivacyReportSanitize.SanitizeReportText(text, stack: true);
    }
}
