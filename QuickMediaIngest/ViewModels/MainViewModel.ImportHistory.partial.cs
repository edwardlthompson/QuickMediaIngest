using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel
    {
        private void SaveImportHistoryRecord(TimeSpan duration)
        {
            try
            {
                var record = new ImportHistoryRecord
                {
                    StartedAtLocal = _importStartedAtUtc == DateTime.MinValue ? DateTime.Now : _importStartedAtUtc.ToLocalTime(),
                    DurationSeconds = Math.Max(0, duration.TotalSeconds),
                    FilesSelected = TotalFilesForImport,
                    FilesImported = CurrentFileBeingImported,
                    FailedFiles = FailedFilesForImport,
                    Source = SelectedSource?.ToString() ?? "Unknown",
                    Destination = DestinationRoot
                };

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ImportHistoryRecords.Insert(0, record);
                    while (ImportHistoryRecords.Count > 50)
                    {
                        ImportHistoryRecords.RemoveAt(ImportHistoryRecords.Count - 1);
                    }
                });

                string path = GetImportHistoryPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Path.GetTempPath());
                string json = JsonSerializer.Serialize(ImportHistoryRecords.ToList());
                File.WriteAllText(path, json);
            }
            catch
            {
                // Ignore history persistence errors.
            }
        }

        private static string GetPendingImportPlanPath()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "pending-import.json");
        }

        public void CheckPendingImportPlan()
        {
            try
            {
                string path = GetPendingImportPlanPath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var plan = JsonSerializer.Deserialize<PendingImportPlan>(json);
                    if (plan != null && plan.SelectedSourcePaths.Count > 0)
                    {
                        HasPendingImportPlan = true;
                        PendingImportPlanDetails = $"{plan.SelectedSourcePaths.Count} files ({plan.SourceDisplay})";
                        return;
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            HasPendingImportPlan = false;
            PendingImportPlanDetails = string.Empty;
        }

        [RelayCommand]
        private void DismissPendingPlanBanner()
        {
            ClearPendingImportPlan();
            HasPendingImportPlan = false;
            PendingImportPlanDetails = string.Empty;
        }

        [RelayCommand]
        private void ResumePendingPlan()
        {
            try
            {
                string path = GetPendingImportPlanPath();
                if (!File.Exists(path))
                {
                    HasPendingImportPlan = false;
                    return;
                }

                string json = File.ReadAllText(path);
                var plan = JsonSerializer.Deserialize<PendingImportPlan>(json);
                if (plan != null)
                {
                    if (!string.IsNullOrWhiteSpace(plan.DestinationRoot))
                    {
                        DestinationRoot = plan.DestinationRoot;
                    }
                    if (!string.IsNullOrWhiteSpace(plan.NamingTemplate))
                    {
                        NamingTemplate = plan.NamingTemplate;
                    }

                    var planSet = new HashSet<string>(plan.SelectedSourcePaths, StringComparer.OrdinalIgnoreCase);
                    foreach (var group in Groups)
                    {
                        foreach (var item in group.Items)
                        {
                            item.IsSelected = planSet.Contains(item.SourcePath);
                        }
                    }

                    DismissPendingPlanBanner();
                    ExecuteImport();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resume pending import plan.");
            }
        }

        private void SavePendingImportPlan(List<ItemGroup> selectedGroups)
        {
            try
            {
                var selectedPaths = selectedGroups
                    .SelectMany(g => g.Items)
                    .Where(i => i.IsSelected)
                    .Select(i => i.SourcePath)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var plan = new PendingImportPlan
                {
                    CreatedAt = DateTime.Now,
                    SourceId = BuildSelectedSourceId(),
                    SourceDisplay = SelectedSource?.ToString() ?? "Unknown",
                    DestinationRoot = DestinationRoot,
                    NamingTemplate = NamingTemplate,
                    SelectedSourcePaths = selectedPaths
                };
                string json = JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(GetPendingImportPlanPath(), json);
                HasPendingImportPlan = true;
                PendingImportPlanDetails = $"{selectedPaths.Count} files ({plan.SourceDisplay})";
            }
            catch
            {
                // Ignore pending plan persistence failures.
            }
        }

        private void ClearPendingImportPlan()
        {
            try
            {
                string path = GetPendingImportPlanPath();
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Ignore
            }
        }

        public void LoadImportHistory()
        {
            try
            {
                string path = GetImportHistoryPath();
                if (!File.Exists(path)) return;
                string json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<List<ImportHistoryRecord>>(json);
                if (loaded == null) return;
                ImportHistoryRecords.Clear();
                foreach (var record in loaded)
                {
                    ImportHistoryRecords.Add(record);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Import history file could not be loaded.");
            }
        }

        public string ExportImportHistoryCsv(string? filter = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("StartedAt,Source,Destination,ImportedFiles,SelectedFiles,FailedFiles,DurationSeconds");

            var query = ImportHistoryRecords.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(r =>
                    (r.Source?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.Destination?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            foreach (var r in query)
            {
                string src = (r.Source ?? string.Empty).Replace("\"", "\"\"");
                string dst = (r.Destination ?? string.Empty).Replace("\"", "\"\"");
                sb.AppendLine($"{r.StartedAtLocal:yyyy-MM-dd HH:mm:ss},\"{src}\",\"{dst}\",{r.FilesImported},{r.FilesSelected},{r.FailedFiles},{r.DurationSeconds:0.##}");
            }

            return sb.ToString();
        }

        private static string GetImportHistoryPath()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest");
            return Path.Combine(folder, "import-history.json");
        }
    }
}
