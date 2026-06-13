using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Media;
using System.Net;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Localization;
using QuickMediaIngest.Core.Services;
using QuickMediaIngest.Data;
using QuickMediaIngest;


namespace QuickMediaIngest.ViewModels
{
    /// <summary>
    /// One entry in the display language list (code = BCP 47, empty = use Windows language).
    /// </summary>
    public sealed class LanguageOption
    {
        public LanguageOption(string code, string label)
        {
            Code = code;
            Label = label;
        }

        public string Code { get; }
        public string Label { get; }
    }

    /// <summary>Quick destination preset row (stable <see cref="Key"/> for config, localized <see cref="Label"/>).</summary>
    public sealed class DestinationPresetOption
    {
        public DestinationPresetOption(string key, string label)
        {
            Key = key;
            Label = label;
        }

        public string Key { get; }
        public string Label { get; }
    }

    /// <summary>One line in the sidebar notification feed.</summary>
    public sealed class NotificationFeedLine
    {
        public required string DisplayText { get; init; }
        public bool UseSuccessAccent { get; init; }
        /// <summary>Visual separator line for a new import session.</summary>
        public bool IsSessionDivider { get; init; }
    }

    /// <summary>
    /// Active filter chip shown in the main toolbar.
    /// </summary>
    public sealed class FilterChipViewModel
    {
        public string Id { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
    }

    public class SidebarOption
    {
        public string Label { get; set; } = string.Empty;
        public ICommand? Command { get; set; }
    }

    public class SidebarSection : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public string Title { get; set; } = string.Empty;
        public ObservableCollection<SidebarOption> Options { get; set; } = new();
        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }
    }

    public class AdbSourceItem
    {
        public string DeviceSerial { get; set; } = "default";
        public override string ToString() => "Android (ADB)";
    }


    public class ImportHistoryRecord
    {
        public DateTime StartedAtLocal { get; set; } = DateTime.Now;
        public double DurationSeconds { get; set; }
        public int FilesSelected { get; set; }
        public int FilesImported { get; set; }
        public int FailedFiles { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;

        public string StartedAtDisplay => StartedAtLocal.ToString("yyyy-MM-dd HH:mm:ss");
        public string DurationDisplay => TimeSpan.FromSeconds(Math.Max(0, DurationSeconds)).ToString(@"hh\:mm\:ss");
        public string SummaryDisplay =>
            $"{StartedAtDisplay} | Imported {FilesImported}/{FilesSelected} | Failed {FailedFiles} | Duration {DurationDisplay}";
    }

    public class FailedImportRecord
    {
        public string SourcePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    internal sealed class QueuedImportJob
    {
        public string SourceId { get; set; } = string.Empty;
        public string SourceDisplay { get; set; } = string.Empty;
        public List<string> SelectedSourcePaths { get; set; } = new();
    }

    internal sealed class PendingImportPlan
    {
        public DateTime CreatedAt { get; set; }
        public string SourceId { get; set; } = string.Empty;
        public string SourceDisplay { get; set; } = string.Empty;
        public string DestinationRoot { get; set; } = string.Empty;
        public string NamingTemplate { get; set; } = string.Empty;
        public List<string> SelectedSourcePaths { get; set; } = new();
    }

    internal sealed class ImportReportArtifact
    {
        public DateTime GeneratedAt { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public double DurationSeconds { get; set; }
        public int FilesSelected { get; set; }
        public int FilesImported { get; set; }
        public int FailedFiles { get; set; }
        public string VerificationMode { get; set; } = string.Empty;
        public string DuplicatePolicy { get; set; } = string.Empty;
        public List<FailedImportRecord> Failed { get; set; } = new();
    }

    internal sealed class UserPreset
    {
        public string Name { get; set; } = string.Empty;
        public string DestinationRoot { get; set; } = string.Empty;
        public string NamingTemplate { get; set; } = string.Empty;
        public string VerificationMode { get; set; } = "Fast";
        public string DuplicatePolicy { get; set; } = "Suffix";
        public string ThumbnailPerformanceMode { get; set; } = "Balanced";
        public bool GroupRawAndRenderedPairs { get; set; } = false;
        public bool ExpandPreviewStacks { get; set; } = false;
        public bool EmbedKeywordsOnImport { get; set; }
        public bool ConfirmBeforeImport { get; set; }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) => _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    public class UpdateIntervalOption
    {
        public string Display { get; set; } = string.Empty;
        public int Hours { get; set; }
    }

    public class DriveSelectionOption
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string DriveType { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public bool IsSelected { get; set; } = true;
    }

    public class ExcludedDriveEntry
    {
        public string DeviceId { get; set; } = string.Empty;
        public string DriveName { get; set; } = string.Empty;
        public string DriveType { get; set; } = string.Empty;
        public string Display => $"{DriveName} ({DriveType})";
    }

    public class SkippedFolderRuleEntry
    {
        public string SourceId { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
        public string Display => $"{SourceId} :: {FolderPath}";
    }
}
