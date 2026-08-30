#nullable enable
using System;
using System.Threading;
using QuickMediaIngest.Core.PrivacyReport;

namespace QuickMediaIngest.Core.CrashCapture;

/// <summary>Opt-in local crash queue. Never auto-sends. Sanitize before persist.</summary>
public sealed class CrashCaptureService
{
    private readonly IPendingCrashStore _store;
    private readonly Func<bool> _isOptIn;
    private int _entered;

    public CrashCaptureService(IPendingCrashStore store, Func<bool> isOptIn)
    {
        _store = store;
        _isOptIn = isOptIn;
    }

    public bool TryCapture(Exception? exception, string? appVersion)
    {
        if (Interlocked.Exchange(ref _entered, 1) == 1)
        {
            return false;
        }

        try
        {
            if (exception is null || !_isOptIn())
            {
                return false;
            }

            string stack = PrivacyReportSanitize.SanitizeReportText(exception.ToString(), stack: true);
            string type = PrivacyReportSanitize.SanitizeReportText(exception.GetType().Name);
            string fingerprint = PrivacyReportFingerprint.FingerprintCrash(stack, type);
            if (_store.IsDiscarded(fingerprint))
            {
                return false;
            }

            var record = new PendingCrash
            {
                Fingerprint = fingerprint,
                ExceptionType = type,
                Stack = stack,
                Description = PrivacyReportSanitize.SanitizeReportText(exception.Message),
                AppVersion = PrivacyReportSanitize.SanitizeReportText(appVersion),
                CreatedAtUtc = DateTime.UtcNow.ToString("O")
            };
            return _store.Replace(record);
        }
        catch
        {
            return false;
        }
        finally
        {
            Interlocked.Exchange(ref _entered, 0);
        }
    }

    public void ApplyOptIn(bool enabled)
    {
        if (!enabled)
        {
            _store.Clear();
        }
    }

    public PendingCrash? Peek() => _store.Load();
}
