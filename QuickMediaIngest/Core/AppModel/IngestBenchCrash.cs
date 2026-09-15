#nullable enable
using QuickMediaIngest.Core.CrashCapture;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Load pending crash into AppModel; Close records discarded fingerprints.</summary>
    public static class IngestBenchCrash
    {
        public static void Load(IngestBenchHost host)
        {
            PendingCrash? crash = host.Crashes.Load();
            if (crash is null || host.Crashes.IsDiscarded(crash.Fingerprint))
            {
                host.Model.ShowCrashOverlay = false;
                host.Model.CrashOverlayText = string.Empty;
                host.Model.CrashFingerprint = string.Empty;
                return;
            }

            host.Model.ShowCrashOverlay = true;
            host.Model.CrashFingerprint = crash.Fingerprint ?? string.Empty;
            host.Model.CrashOverlayText = string.IsNullOrWhiteSpace(crash.Description)
                ? crash.ExceptionType
                : crash.Description;
        }

        public static void Dismiss(IngestBenchHost host)
        {
            if (!string.IsNullOrWhiteSpace(host.Model.CrashFingerprint))
            {
                host.Crashes.MarkDiscarded(host.Model.CrashFingerprint);
            }
            else
            {
                host.Crashes.Clear();
            }

            host.Model.ShowCrashOverlay = false;
            host.Model.CrashOverlayText = string.Empty;
            host.Model.CrashFingerprint = string.Empty;
        }
    }
}
