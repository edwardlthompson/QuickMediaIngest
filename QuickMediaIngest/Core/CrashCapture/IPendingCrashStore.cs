#nullable enable
namespace QuickMediaIngest.Core.CrashCapture;

public interface IPendingCrashStore
{
    PendingCrash? Load();
    bool Replace(PendingCrash record);
    void Clear();
}
