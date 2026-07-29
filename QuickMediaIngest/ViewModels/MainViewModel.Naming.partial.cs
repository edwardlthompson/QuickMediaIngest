using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QuickMediaIngest.ViewModels
{
    /// <summary>File naming preferences and preview (partial MainViewModel).</summary>
    public partial class MainViewModel
    {
        public ObservableCollection<string> NamingPresetOptions { get; } = new ObservableCollection<string>
        {
            "Recommended (Date + Shoot + Original)",
            "Date + Time + Shoot + Original",
            "Shoot + Date + Original",
            "Custom"
        };
        public ObservableCollection<string> NamingDateFormatOptions { get; } = new ObservableCollection<string>
        {
            "yyyy-MM-dd",
            "yyyyMMdd"
        };
        public ObservableCollection<string> NamingTimeFormatOptions { get; } = new ObservableCollection<string>
        {
            "HH-mm-ss",
            "HHmmss",
            "HH-mm-ss-fff",
            "HHmmssfff"
        };
        public ObservableCollection<string> NamingSeparatorOptions { get; } = new ObservableCollection<string>
        {
            "_",
            "-"
        };
        public ObservableCollection<string> NamingPreviewExamples { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableTokens { get; } = new ObservableCollection<string>
        {
            "[Date]", "[Time]", "[TimeMs]", "[YYYY]", "[MM]", "[DD]", "[HH]", "[mm]", "[ss]", "[fff]", "[ShootName]", "[Original]", "[Sequence]", "[Ext]", "_", "-"
        };
        public ObservableCollection<TokenItem> SelectedTokens { get; } = new ObservableCollection<TokenItem>();
        public void UpdateNamingFromTokens()
        {
            NamingTemplate = string.Join("", SelectedTokens.Select(t => t.Value));
            OnPropertyChanged("NamingTemplate");
            SaveConfig();
        }
        private void ApplyNamingPreset(string preset)
        {
            if (_loadingConfig)
            {
                return;
            }

            _applyingNamingPreset = true;
            try
            {
                _updatingNamingFromUi = true;
                switch (preset)
                {
                    case "Recommended (Date + Shoot + Original)":
                        NamingIncludeDate = true;
                        NamingIncludeTime = false;
                        NamingIncludeSequence = false;
                        NamingIncludeShootName = true;
                        NamingIncludeOriginalName = true;
                        break;
                    case "Date + Time + Shoot + Original":
                        NamingIncludeDate = true;
                        NamingIncludeTime = true;
                        NamingIncludeSequence = false;
                        NamingIncludeShootName = true;
                        NamingIncludeOriginalName = true;
                        break;
                    case "Shoot + Date + Original":
                        NamingIncludeDate = true;
                        NamingIncludeTime = false;
                        NamingIncludeSequence = false;
                        NamingIncludeShootName = true;
                        NamingIncludeOriginalName = true;
                        break;
                    default:
                        // Custom keeps user-selected options.
                        break;
                }
                _updatingNamingFromUi = false;
                UpdateNamingTemplateFromOptions();
            }
            finally
            {
                _applyingNamingPreset = false;
            }
        }
        private void UpdateNamingTemplateFromOptions()
        {
            if (_updatingNamingFromUi || _loadingConfig)
            {
                return;
            }

            var parts = new List<string>();
            if (NamingIncludeDate)
            {
                parts.Add(NamingDateFormat == "yyyyMMdd" ? "[YYYY][MM][DD]" : "[Date]");
            }
            if (NamingIncludeTime)
            {
                parts.Add(NamingTimeFormat switch
                {
                    "HHmmss" => "[HH][mm][ss]",
                    "HH-mm-ss-fff" => "[TimeMs]",
                    "HHmmssfff" => "[HH][mm][ss][fff]",
                    _ => "[Time]"
                });
            }
            if (NamingIncludeSequence)
            {
                parts.Add("[Sequence]");
            }
            if (NamingIncludeShootName)
            {
                parts.Add("[ShootName]");
            }
            if (NamingIncludeOriginalName)
            {
                parts.Add("[Original]");
            }

            if (parts.Count == 0)
            {
                parts.Add("[Original]");
            }

            _updatingNamingFromUi = true;
            NamingTemplate = string.Join(NamingSeparator, parts);
            _updatingNamingFromUi = false;

            // Checkbox/format edits that diverge from a named preset must stick as Custom
            // so the next launch does not re-apply "Recommended" and wipe the template.
            if (!_applyingNamingPreset &&
                !string.Equals(NamingPreset, "Custom", StringComparison.Ordinal))
            {
                NamingPreset = "Custom";
            }

            RefreshNamingPreviewExamples();
            SaveConfig();
        }
        /// <summary>
        /// If the saved preset label no longer matches the saved template, coerce to Custom
        /// without rebuilding the template (load-time / post-load safety).
        /// </summary>
        private void CoerceNamingPresetToCustomIfTemplateDiverged()
        {
            if (string.Equals(NamingPreset, "Custom", StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(NamingTemplate))
            {
                return;
            }

            string expected = BuildNamingTemplateForPreset(NamingPreset);
            if (!string.Equals(NamingTemplate, expected, StringComparison.Ordinal))
            {
                NamingPreset = "Custom";
            }
        }
        private string BuildNamingTemplateForPreset(string preset)
        {
            bool includeDate = true;
            bool includeTime = false;
            bool includeSequence = false;
            bool includeShoot = true;
            bool includeOriginal = true;

            switch (preset)
            {
                case "Date + Time + Shoot + Original":
                    includeTime = true;
                    break;
                case "Shoot + Date + Original":
                    break;
                case "Recommended (Date + Shoot + Original)":
                    break;
                default:
                    return NamingTemplate;
            }

            var parts = new List<string>();
            if (includeDate)
            {
                parts.Add(NamingDateFormat == "yyyyMMdd" ? "[YYYY][MM][DD]" : "[Date]");
            }
            if (includeTime)
            {
                parts.Add(NamingTimeFormat switch
                {
                    "HHmmss" => "[HH][mm][ss]",
                    "HH-mm-ss-fff" => "[TimeMs]",
                    "HHmmssfff" => "[HH][mm][ss][fff]",
                    _ => "[Time]"
                });
            }
            if (includeSequence)
            {
                parts.Add("[Sequence]");
            }
            if (includeShoot)
            {
                parts.Add("[ShootName]");
            }
            if (includeOriginal)
            {
                parts.Add("[Original]");
            }

            string sep = string.IsNullOrEmpty(NamingSeparator) ? "_" : NamingSeparator;
            return string.Join(sep, parts);
        }
        private void SyncNamingOptionsFromTemplate()
        {
            _updatingNamingFromUi = true;
            NamingIncludeDate = NamingTemplate.Contains("[Date]", StringComparison.Ordinal) ||
                               (NamingTemplate.Contains("[YYYY]", StringComparison.Ordinal) &&
                                NamingTemplate.Contains("[MM]", StringComparison.Ordinal) &&
                                NamingTemplate.Contains("[DD]", StringComparison.Ordinal));
            NamingDateFormat = NamingTemplate.Contains("[YYYY][MM][DD]", StringComparison.Ordinal) ? "yyyyMMdd" : "yyyy-MM-dd";
            NamingIncludeTime = NamingTemplate.Contains("[Time]", StringComparison.Ordinal) ||
                                NamingTemplate.Contains("[TimeMs]", StringComparison.Ordinal) ||
                                (NamingTemplate.Contains("[HH]", StringComparison.Ordinal) &&
                                 NamingTemplate.Contains("[mm]", StringComparison.Ordinal) &&
                                 NamingTemplate.Contains("[ss]", StringComparison.Ordinal));
            NamingTimeFormat = NamingTemplate.Contains("[HH][mm][ss][fff]", StringComparison.Ordinal) ? "HHmmssfff"
                : NamingTemplate.Contains("[TimeMs]", StringComparison.Ordinal) ? "HH-mm-ss-fff"
                : NamingTemplate.Contains("[HH][mm][ss]", StringComparison.Ordinal) ? "HHmmss"
                : "HH-mm-ss";
            NamingIncludeSequence = NamingTemplate.Contains("[Sequence]", StringComparison.Ordinal);
            NamingIncludeShootName = NamingTemplate.Contains("[ShootName]", StringComparison.Ordinal);
            NamingIncludeOriginalName = NamingTemplate.Contains("[Original]", StringComparison.Ordinal);
            NamingSeparator = NamingTemplate.Contains("-") && !NamingTemplate.Contains("_") ? "-" : "_";
            _updatingNamingFromUi = false;
        }
        private void RefreshNamingPreviewExamples()
        {
            try
            {
                string separator = string.IsNullOrWhiteSpace(NamingSeparator) ? "_" : NamingSeparator;
                string date = NamingDateFormat == "yyyyMMdd" ? "20260425" : "2026-04-25";
                string time = NamingTimeFormat switch
                {
                    "HHmmss" => "195649",
                    "HH-mm-ss-fff" => "19-56-49-123",
                    "HHmmssfff" => "195649123",
                    _ => "19-56-49"
                };
                string shoot = string.IsNullOrWhiteSpace(NamingShootNameSample) ? "my-shoot" : NamingShootNameSample.Trim();
                string[] originals = { "img_0001", "img_0002", "img_0003" };

                string template = NamingTemplate;
                if (string.IsNullOrWhiteSpace(template))
                {
                    template = "[Date]" + separator + "[ShootName]" + separator + "[Original]";
                }

                NamingPreviewExamples.Clear();
                for (int index = 0; index < originals.Length; index++)
                {
                    string original = originals[index];
                    string output = template
                        .Replace("[Date]", date, StringComparison.Ordinal)
                        .Replace("[Time]", time, StringComparison.Ordinal)
                        .Replace("[TimeMs]", "19-56-49-123", StringComparison.Ordinal)
                        .Replace("[YYYY]", "2026", StringComparison.Ordinal)
                        .Replace("[MM]", "04", StringComparison.Ordinal)
                        .Replace("[DD]", "25", StringComparison.Ordinal)
                        .Replace("[HH]", "19", StringComparison.Ordinal)
                        .Replace("[mm]", "56", StringComparison.Ordinal)
                        .Replace("[ss]", "49", StringComparison.Ordinal)
                        .Replace("[fff]", "123", StringComparison.Ordinal)
                        .Replace("[ShootName]", shoot, StringComparison.Ordinal)
                        .Replace("[Original]", original, StringComparison.Ordinal)
                        .Replace("[Sequence]", (index + 1).ToString("D4"), StringComparison.Ordinal)
                        .Replace("[Ext]", "jpg", StringComparison.Ordinal)
                        .Replace("__", "_", StringComparison.Ordinal)
                        .Replace("--", "-", StringComparison.Ordinal)
                        .Trim('_', '-');

                    if (NamingLowercase)
                    {
                        output = output.ToLowerInvariant();
                    }

                    NamingPreviewExamples.Add($"{output}.jpg");
                }
            }
            catch
            {
                // Keep UI stable if preview generation fails.
            }
        }
    }
}
