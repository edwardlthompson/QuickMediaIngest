#nullable enable
using QuickMediaIngest.Core.AppModel;

namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel
    {
        public IngestBenchAppModel Bench { get; } = new();

        public bool IsFirstRun
        {
            get => Bench.IsFirstRun;
            set
            {
                if (Bench.IsFirstRun == value)
                {
                    return;
                }

                Bench.IsFirstRun = value;
                OnPropertyChanged(nameof(IsFirstRun));
            }
        }

        internal void SyncIngestBench()
        {
            bool wasFirstRun = Bench.IsFirstRun;
            Bench.DeleteAfterImport = DeleteAfterImport;
            Bench.IsImporting = IsImporting;
            Bench.DestinationRoot = DestinationRoot ?? string.Empty;
            Bench.SourceCount = Sources.Count;
            if (Sources.Count > 0)
            {
                Bench.IsFirstRun = false;
            }

            if (wasFirstRun != Bench.IsFirstRun)
            {
                OnPropertyChanged(nameof(IsFirstRun));
            }
        }
    }
}
