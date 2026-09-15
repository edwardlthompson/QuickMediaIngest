#nullable enable
namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Status-bar job meter (scan / thumbnails / import). Never a full-screen scrim.</summary>
    public static class IngestBenchActivity
    {
        public const string Scan = "scan";
        public const string Thumbs = "thumbs";
        public const string Import = "import";

        public static void Begin(IngestBenchAppModel model, string kind, string status, bool indeterminate = true)
        {
            model.ActivityKind = kind ?? string.Empty;
            model.ActivityIndeterminate = indeterminate;
            model.ImportPercent = 0;
            model.ImportStatus = status ?? string.Empty;
        }

        public static void Begin(IngestBenchHost host, string kind, string status, bool indeterminate = true) =>
            host.Post(() => Begin(host.Model, kind, status, indeterminate));

        public static void Report(IngestBenchAppModel model, int percent, string status)
        {
            model.ActivityIndeterminate = false;
            model.ImportPercent = ImportUi.ImportProgressScene.ClampPercent(percent);
            model.ImportStatus = status ?? string.Empty;
        }

        public static void Report(IngestBenchHost host, int percent, string status) =>
            host.Post(() => Report(host.Model, percent, status));

        public static void Idle(IngestBenchAppModel model)
        {
            model.ActivityKind = string.Empty;
            model.ActivityIndeterminate = false;
            model.ImportPercent = 0;
        }

        public static void Idle(IngestBenchHost host) => host.Post(() => Idle(host.Model));

        public static string Counts(string job, int succeeded, int failed) =>
            (job ?? "Job") + " " + succeeded + " succeeded, " + failed + " failed.";
    }
}
