#nullable enable
namespace QuickMediaIngest.Data
{
    public interface IDatabaseService
    {
        /// <summary>Runs occasional <c>VACUUM</c> to control DB file growth.</summary>
        void TryPeriodicVacuum(int minimumDaysBetweenRuns = 14);
    }
}
