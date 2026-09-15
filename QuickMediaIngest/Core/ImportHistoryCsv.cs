#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    /// <summary>Filter and CSV export for import history (both heads).</summary>
    public static class ImportHistoryCsv
    {
        public const string Header = "StartedAt,DurationSeconds,FilesSelected,FilesImported,FailedFiles,Source,Destination";

        public static bool Matches(ImportHistoryRecord record, string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            string needle = filter.Trim();
            return Contains(record.Source, needle)
                || Contains(record.Destination, needle)
                || Contains(record.StartedAtDisplay, needle)
                || Contains(record.SummaryDisplay, needle);
        }

        public static IEnumerable<ImportHistoryRecord> Filter(
            IEnumerable<ImportHistoryRecord> records,
            string? filter) =>
            records.Where(r => Matches(r, filter));

        public static string ToCsv(IEnumerable<ImportHistoryRecord> records, string? filter = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Header);
            foreach (ImportHistoryRecord r in Filter(records, filter))
            {
                sb.Append(Escape(r.StartedAtLocal.ToString("yyyy-MM-dd HH:mm:ss")));
                sb.Append(',');
                sb.Append(Escape(r.DurationSeconds.ToString("0.##")));
                sb.Append(',');
                sb.Append(r.FilesSelected);
                sb.Append(',');
                sb.Append(r.FilesImported);
                sb.Append(',');
                sb.Append(r.FailedFiles);
                sb.Append(',');
                sb.Append(Escape(r.Source));
                sb.Append(',');
                sb.Append(Escape(r.Destination));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static bool Contains(string? haystack, string needle) =>
            haystack?.Contains(needle, StringComparison.OrdinalIgnoreCase) == true;

        private static string Escape(string? value)
        {
            string s = value ?? string.Empty;
            if (s.Contains('"'))
            {
                s = s.Replace("\"", "\"\"", StringComparison.Ordinal);
            }

            if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
            {
                return $"\"{s}\"";
            }

            return s;
        }
    }
}
