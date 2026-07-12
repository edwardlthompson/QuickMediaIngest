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
using QuickMediaIngest.Thumbnails;


namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {

        private void ImportItem_SelectionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ImportItem.IsSelected))
            {
                RefreshImportReadinessSummary();
            }
        }

        private void DetachImportItemSelectionHandlers()
        {
            foreach (var group in Groups)
            {
                foreach (var item in group.Items)
                {
                    item.PropertyChanged -= ImportItem_SelectionChanged;
                }
            }
        }

        private void AttachImportItemSelectionHandlers(ItemGroup group)
        {
            foreach (var item in group.Items)
            {
                item.PropertyChanged -= ImportItem_SelectionChanged;
                item.PropertyChanged += ImportItem_SelectionChanged;
            }
        }

        private string BuildImportConfirmationMessage(List<ItemGroup> selectedGroups, int totalFiles)
        {
            long bytes = selectedGroups.SelectMany(g => g.Items).Where(i => i.IsSelected).Sum(i => Math.Max(0, i.FileSize));
            string mb = (bytes / (1024d * 1024d)).ToString("0.00", CultureInfo.CurrentCulture);

            var sb = new StringBuilder();
            sb.AppendLine(AppLocalizer.Format("Vm_ConfirmImport_Line1", totalFiles, mb));
            sb.AppendLine();
            sb.AppendLine(AppLocalizer.Get("Vm_ConfirmImport_Destination"));
            sb.AppendLine(DestinationRoot);
            sb.AppendLine();
            sb.Append(AppLocalizer.Get("Vm_ConfirmImport_DupPolicy")).Append(' ').AppendLine(DuplicatePolicy);
            sb.Append(AppLocalizer.Get("Vm_ConfirmImport_Verify")).Append(' ').AppendLine(VerificationMode);
            sb.Append(AppLocalizer.Get("Vm_ConfirmImport_DeleteAfter")).Append(' ')
                .AppendLine(DeleteAfterImport ? AppLocalizer.Get("Vm_Yes") : AppLocalizer.Get("Vm_No"));
            sb.AppendLine();
            sb.AppendLine(AppLocalizer.Get("Vm_ConfirmImport_KeywordsHeader"));
            if (!EmbedKeywordsOnImport)
            {
                sb.AppendLine(AppLocalizer.Get("Vm_Readiness_KwOff"));
            }
            else
            {
                bool any = false;
                foreach (ItemGroup g in selectedGroups.OrderBy(x => x.Title))
                {
                    List<string> list = KeywordInputParser.Parse(g.KeywordsText);
                    if (list.Count == 0)
                    {
                        continue;
                    }

                    any = true;
                    sb.AppendLine(AppLocalizer.Format("Vm_Confirm_ShootKeywords", g.Title, string.Join(", ", list)));
                }

                if (!any)
                {
                    sb.AppendLine(AppLocalizer.Get("Vm_ConfirmImport_NoKeywords"));
                }
            }

            return sb.ToString().TrimEnd();
        }

        private void RepopulateLanguageOptions()
        {
            UiLanguageOptions.Clear();
            UiLanguageOptions.Add(new LanguageOption("", AppLocalizer.Get("Lang_UseSystem")));
            UiLanguageOptions.Add(new LanguageOption("en", AppLocalizer.Get("Lang_English")));
            UiLanguageOptions.Add(new LanguageOption("fr", AppLocalizer.Get("Lang_French")));
            UiLanguageOptions.Add(new LanguageOption("es", AppLocalizer.Get("Lang_Spanish")));
            InitializeIntervalOptions();
            InitializeSidebarSections();
            ApplyLocalizedShellStrings();
        }

        [RelayCommand]
        private async Task RetryFailedPreviewLoadsAsync()
        {
            if (Groups.Count == 0 || SelectedSource == null)
            {
                StatusMessage = AppLocalizer.Get("Vm_Status_NothingToRetry");
                return;
            }

            var failedItems = Groups.SelectMany(g => g.Items).Where(i => i.ThumbnailPreviewStatus == ThumbnailPreviewStatus.Failed).ToList();
            if (failedItems.Count == 0)
            {
                StatusMessage = AppLocalizer.Get("Vm_Status_NoFailedPreviews");
                return;
            }

            StatusMessage = $"Retrying {failedItems.Count} preview(s)...";

            var failedLocal = failedItems.Where(i => !i.IsFtpSource).ToList();
            var failedFtp = failedItems.Where(i => i.IsFtpSource).ToList();

            if (failedLocal.Count > 0)
            {
                await Task.Run(() =>
                {
                    Parallel.ForEach(failedLocal, new ParallelOptions { MaxDegreeOfParallelism = GetThumbnailWorkerCount() }, item =>
                    {
                        string key = BuildItemKey(item);
                        object? thumb = null;
                        try
                        {
                            thumb = WpfThumbnailBridge.ToBitmapSource(
                                _thumbnailService.GetThumbnail(item.SourcePath, BuildThumbnailHints()));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Retry thumbnail failed for {Path}.", item.SourcePath);
                        }

                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (thumb != null)
                            {
                                item.Thumbnail = thumb;
                                item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Loaded;
                                _thumbnailByItemKey[key] = thumb;
                            }
                            else
                            {
                                item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Failed;
                            }
                        });
                    });
                });
            }

            if (failedFtp.Count > 0 && SelectedSource is FtpSourceItem ftpSource)
            {
                await LoadFtpThumbnailBatchAsync(failedFtp, ftpSource, failedFtp.Count, 0, false);
            }
            else if (failedFtp.Count > 0 && SelectedSource is UnifiedSourceItem)
            {
                var ftpSourcesByKey = Sources
                    .OfType<FtpSourceItem>()
                    .ToDictionary(BuildSourceKey, f => f, StringComparer.OrdinalIgnoreCase);

                foreach (var group in failedFtp.GroupBy(i => i.SourceId, StringComparer.OrdinalIgnoreCase))
                {
                    if (!ftpSourcesByKey.TryGetValue(group.Key, out var ftp))
                    {
                        continue;
                    }

                    await LoadFtpThumbnailBatchAsync(group.ToList(), ftp, group.Count(), 0, false);
                }
            }

            await Application.Current.Dispatcher.InvokeAsync(RefreshPreviewHealthSummary);
            StatusMessage = AppLocalizer.Get("Vm_Status_PreviewRetryFinished");
        }

        [RelayCommand]
        private async Task ClearThumbnailCacheAndReloadPreviewsAsync()
        {
            if (SelectedSource == null || Groups.Count == 0)
            {
                StatusMessage = AppLocalizer.Get("Vm_Status_LoadSourceBeforeClearPreviewCache");
                return;
            }

            try
            {
                _thumbnailByItemKey.Clear();
                ClearThumbnailDiskCache();
                foreach (var group in Groups)
                {
                    foreach (var item in group.Items)
                    {
                        item.Thumbnail = null;
                        item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Unknown;
                    }
                }

                string label = GetThumbnailSourceLabel();
                StatusMessage = AppLocalizer.Get("Vm_Status_ThumbnailCacheClearedReloading");
                await LoadThumbnailsAsync(Groups.ToList(), SelectedSource, label);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Unable to reload previews: {ex.Message}";
            }
        }

        private string GetThumbnailSourceLabel()
        {
            return SelectedSource switch
            {
                FtpSourceItem ftp => $"{ftp.Host}{NormalizeFtpPath(ftp.RemoteFolder)}",
                UnifiedSourceItem => "Unified",
                _ => SelectedSource?.ToString() ?? "source"
            };
        }
    }
}
