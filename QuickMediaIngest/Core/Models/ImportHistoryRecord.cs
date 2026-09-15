#nullable enable
using System;
using System.Text.Json.Serialization;

namespace QuickMediaIngest.Core.Models
{
    /// <summary>One completed import session (persisted in import-history.json).</summary>
    public sealed class ImportHistoryRecord
    {
        public DateTime StartedAtLocal { get; set; } = DateTime.Now;

        public double DurationSeconds { get; set; }

        public int FilesSelected { get; set; }

        public int FilesImported { get; set; }

        public int FailedFiles { get; set; }

        public string Source { get; set; } = string.Empty;

        public string Destination { get; set; } = string.Empty;

        [JsonIgnore]
        public string StartedAtDisplay => StartedAtLocal.ToString("yyyy-MM-dd HH:mm:ss");

        [JsonIgnore]
        public string DurationDisplay => TimeSpan.FromSeconds(Math.Max(0, DurationSeconds)).ToString(@"hh\:mm\:ss");

        [JsonIgnore]
        public string SummaryDisplay =>
            $"{StartedAtDisplay} | Imported {FilesImported}/{FilesSelected} | Failed {FailedFiles} | Duration {DurationDisplay}";
    }
}
