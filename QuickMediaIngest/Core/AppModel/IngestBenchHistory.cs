#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Import history overlay: load, append, filter, CSV, clear.</summary>
    public static class IngestBenchHistory
    {
        public static void Load(IngestBenchHost host)
        {
            IngestBenchAppModel model = host.Model;
            model.HistoryRecords.Clear();
            foreach (ImportHistoryRecord record in new ImportHistoryStore(host.Paths).Load())
            {
                model.HistoryRecords.Add(record);
            }

            ApplyFilter(host);
        }

        public static void Record(
            IngestBenchHost host,
            int selected,
            int imported,
            int failed,
            TimeSpan duration)
        {
            IngestBenchAppModel model = host.Model;
            model.HistoryRecords.Insert(0, new ImportHistoryRecord
            {
                StartedAtLocal = DateTime.Now,
                DurationSeconds = Math.Max(0, duration.TotalSeconds),
                FilesSelected = Math.Max(0, selected),
                FilesImported = Math.Max(0, imported),
                FailedFiles = Math.Max(0, failed),
                Source = host.ScanRoots.FirstOrDefault() ?? "Local",
                Destination = model.DestinationRoot ?? string.Empty,
            });
            while (model.HistoryRecords.Count > ImportHistoryStore.Cap)
            {
                model.HistoryRecords.RemoveAt(model.HistoryRecords.Count - 1);
            }

            new ImportHistoryStore(host.Paths).Save(model.HistoryRecords.ToList());
            ApplyFilter(host);
        }

        public static void ApplyFilter(IngestBenchHost host)
        {
            IngestBenchAppModel model = host.Model;
            model.VisibleHistory.Clear();
            foreach (ImportHistoryRecord record in ImportHistoryCsv.Filter(model.HistoryRecords, model.HistoryFilter))
            {
                model.VisibleHistory.Add(record);
            }
        }

        public static string ExportCsv(IngestBenchHost host) =>
            ImportHistoryCsv.ToCsv(host.Model.HistoryRecords, host.Model.HistoryFilter);

        public static async Task<bool> ClearAsync(IngestBenchHost host, IngestBenchCopy copy)
        {
            bool ok = await host.Prompt.ConfirmAsync(
                copy.Get("Msg_ClearImportHistory_Title"),
                copy.Get("Msg_ClearImportHistory_Body"));
            if (!ok)
            {
                return false;
            }

            host.Model.HistoryRecords.Clear();
            new ImportHistoryStore(host.Paths).Clear();
            ApplyFilter(host);
            return true;
        }
    }
}
