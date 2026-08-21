#nullable enable

namespace QuickMediaIngest.Core
{
    /// <summary>Device-local persistence for donate nudge and update-check timestamps.</summary>
    public interface IUpdateDonateStore
    {
        UpdateDonatePreferences Load();
        void Save(UpdateDonatePreferences preferences);
    }
}
