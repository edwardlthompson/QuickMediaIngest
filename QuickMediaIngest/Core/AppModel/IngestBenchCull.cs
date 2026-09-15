#nullable enable
using System.Linq;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Pick/reject, stars, and color labels for ingest-bench review.</summary>
    public static class IngestBenchCull
    {
        public static readonly int[] Ratings = { 0, 1, 2, 3, 4, 5 };

        public static readonly string[] ColorNames = { "", "Red", "Yellow", "Green", "Blue", "Purple" };

        public static void Remember(IngestBenchHost host) =>
            CullSelectionPersistence.Snapshot(host.Groups.SelectMany(g => g.Items));

        public static void PickSelected(IngestBenchHost host)
        {
            host.Model.SelectedCullItem?.Pick();
            Remember(host);
        }

        public static void RejectSelected(IngestBenchHost host)
        {
            host.Model.SelectedCullItem?.Reject();
            Remember(host);
        }

        public static void SetRating(IngestBenchHost host, int rating)
        {
            if (host.Model.SelectedCullItem is null)
            {
                return;
            }

            host.Model.SelectedCullItem.Rating = rating;
            Remember(host);
        }

        public static void SetColor(IngestBenchHost host, string? color)
        {
            if (host.Model.SelectedCullItem is null)
            {
                return;
            }

            host.Model.SelectedCullItem.ColorLabel = color ?? string.Empty;
            Remember(host);
        }
    }
}
