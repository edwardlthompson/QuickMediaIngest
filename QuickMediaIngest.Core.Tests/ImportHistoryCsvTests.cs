#nullable enable
using System;
using System.IO;
using System.Threading.Tasks;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Prompt;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class ImportHistoryCsvTests
{
    [Fact]
    public void Filter_MatchesSourceOrDestination()
    {
        var rows = new[]
        {
            new ImportHistoryRecord { Source = "SD card", Destination = "/tmp/a" },
            new ImportHistoryRecord { Source = "FTP", Destination = "/tmp/b" },
        };
        Assert.Single(ImportHistoryCsv.Filter(rows, "ftp"));
        Assert.Empty(ImportHistoryCsv.Filter(rows, "none"));
        Assert.Equal(2, ImportHistoryCsv.Filter(rows, " ").Count());
    }

    [Fact]
    public void ToCsv_EscapesCommasAndQuotes()
    {
        var row = new ImportHistoryRecord
        {
            StartedAtLocal = new DateTime(2026, 1, 2, 3, 4, 5),
            DurationSeconds = 1.5,
            FilesSelected = 2,
            FilesImported = 2,
            FailedFiles = 0,
            Source = "cam, \"wifi\"",
            Destination = "/tmp/out",
        };
        string csv = ImportHistoryCsv.ToCsv(new[] { row }, "wifi");
        Assert.Contains(ImportHistoryCsv.Header, csv);
        Assert.Contains("\"cam, \"\"wifi\"\"\"", csv);
        Assert.DoesNotContain("FTP", csv);
    }

    [Fact]
    public void Store_RoundTripsAndCaps()
    {
        string root = Path.Combine(Path.GetTempPath(), "qmi-hist-" + Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        var store = new ImportHistoryStore(new TempHistPaths(root));
        var rows = new ImportHistoryRecord[ImportHistoryStore.Cap + 3];
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = new ImportHistoryRecord { Source = "s" + i, Destination = "/d" };
        }

        store.Save(rows);
        var loaded = store.Load();
        Assert.Equal(ImportHistoryStore.Cap, loaded.Count);
        Assert.Equal("s0", loaded[0].Source);
        store.Clear();
        Assert.Empty(store.Load());
    }

    [Fact]
    public async Task Host_LoadsHistory_AndClearRespectsPrompt()
    {
        string root = Path.Combine(Path.GetTempPath(), "qmi-hist-host-" + Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        var paths = new TempHistPaths(root);
        var store = new ImportHistoryStore(paths);
        store.Save(new[] { new ImportHistoryRecord { Source = "card", Destination = "/pix" } });
        IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        Assert.Single(host.Model.HistoryRecords);
        host.Model.HistoryFilter = "nope";
        IngestBenchHistory.ApplyFilter(host);
        Assert.Empty(host.Model.VisibleHistory);
        host.Model.HistoryFilter = "card";
        IngestBenchHistory.ApplyFilter(host);
        Assert.Single(host.Model.VisibleHistory);
        Assert.Contains("card", IngestBenchHistory.ExportCsv(host));
        bool cleared = await IngestBenchHistory.ClearAsync(host, IngestBenchCopy.Neutral);
        Assert.True(cleared);
        Assert.Empty(host.Model.HistoryRecords);
    }

    private sealed class TempHistPaths : IAppPaths
    {
        public TempHistPaths(string root) => AppDataRoot = root;

        public string AppDataRoot { get; }
    }
}
