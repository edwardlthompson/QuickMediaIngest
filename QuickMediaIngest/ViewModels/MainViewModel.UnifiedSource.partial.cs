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

        private static void StampItems(List<ImportItem> items, string sourceId, bool isFtp)
        {
            foreach (var item in items)
            {
                item.SourceId = sourceId;
                item.IsFtpSource = isFtp;
            }
        }

        private static List<ImportItem> CloneItems(List<ImportItem> items)
        {
            return items.Select(i => new ImportItem
            {
                SourcePath = i.SourcePath,
                SourceId = i.SourceId,
                IsFtpSource = i.IsFtpSource,
                FileName = i.FileName,
                FileSize = i.FileSize,
                DateTaken = i.DateTaken,
                IsVideo = i.IsVideo,
                FileType = i.FileType,
                IsSelected = i.IsSelected,
                Thumbnail = i.Thumbnail,
                IsPreviewVisible = i.IsPreviewVisible,
                PreviewLabel = i.PreviewLabel,
                StackKey = i.StackKey,
                IsStackRepresentative = i.IsStackRepresentative,
                ThumbnailPreviewStatus = i.ThumbnailPreviewStatus
            }).ToList();
        }

        private static string BuildItemKey(ImportItem item)
        {
            string sourceId = string.IsNullOrWhiteSpace(item.SourceId) ? "unknown" : item.SourceId;
            return $"{sourceId}|{item.SourcePath}";
        }

        private int GetThumbnailWorkerCount(string? samplePath = null)
        {
            int cpu = Math.Max(2, Environment.ProcessorCount);
            int workers = ThumbnailPerformanceMode switch
            {
                "Low" => 2,
                "Max" => Math.Clamp(cpu, 6, 16),
                "Ultra" => Math.Clamp(cpu * 2, 12, 32),
                _ => Math.Clamp(Math.Max(3, cpu / 2), 3, 12)
            };
            return RemovableDriveIo.CapPreviewWorkers(workers, samplePath);
        }

        private int GetFtpThumbnailWorkerCount()
        {
            int cpu = Math.Max(2, Environment.ProcessorCount);
            return ThumbnailPerformanceMode switch
            {
                "Low" => 2,
                "Max" => Math.Clamp(cpu, 4, 10),
                "Ultra" => Math.Clamp(cpu * 2, 8, 16),
                _ => Math.Clamp(Math.Max(3, cpu / 2), 3, 6)
            };
        }

        private ThumbnailHints? BuildThumbnailHints()
        {
            int deferMs = ThumbnailPerformanceMode switch
            {
                "Low" => 48,
                "Max" => 0,
                "Ultra" => 0,
                _ => 18
            };

            return deferMs > 0 ? new ThumbnailHints { DeferRawShellMilliseconds = deferMs } : null;
        }

        private FtpThumbnailLoadOptions BuildFtpThumbnailLoadOptions()
        {
            AdbTransferSession? session = null;
            IAdbPreviewFetcher? fetcher = null;
            IAdbVideoThumbnailFetcher? videoFetcher = null;
            if (PreferAdbTransferWhenAvailable)
            {
                string folder = SelectedSource is FtpSourceItem ftpSel
                    ? NormalizeFtpPath(ftpSel.RemoteFolder)
                    : NormalizeFtpPath(string.IsNullOrWhiteSpace(FtpRemoteFolder) ? "/DCIM" : FtpRemoteFolder);
                session = AdbTransferEligibility.TryResolve(folder, _adbPathProbe);
                if (session != null)
                {
                    fetcher = _adbPreviewFetcher;
                    videoFetcher = _adbVideoThumbnailFetcher;
                }
            }

            // Cap parallel RETRs to reduce phone FTP "Connection reset" from early-close capped reads.
            // PreferAdb also forces Balanced so FluentFTP pools stay off for thumbs.
            return new FtpThumbnailLoadOptions
            {
                DownloadParallelism = Math.Min(GetFtpThumbnailWorkerCount(), 3),
                DecodeParallelism = GetThumbnailWorkerCount(),
                PerformanceMode = session != null ? "Balanced" : ThumbnailPerformanceMode,
                AdbSession = session,
                AdbPreviewFetcher = fetcher,
                AdbVideoThumbnailFetcher = videoFetcher,
                AdbPathProbe = session != null ? _adbPathProbe : null,
            };
        }

        private static List<ImportItem> OrderItemsForViewportPriority(List<ItemGroup> groups)
        {
            return groups
                .Select((group, index) => (group, index))
                .OrderByDescending(x => x.group.IsExpanded)
                .ThenBy(x => x.index)
                .SelectMany(x => x.group.Items)
                .ToList();
        }

        private static string BuildSourceKey(FtpSourceItem ftp)
        {
            return FtpPathNormalizer.BuildFtpSourceKey(ftp.Host, ftp.Port, ftp.RemoteFolder);
        }

        private static string BuildSourceKey(string localPath)
        {
            return $"local|{localPath}";
        }

        private async void ExecuteBuildSelectedPreviews()
        {
            if (SelectedSource == null || Groups.Count == 0)
            {
                StatusMessage = AppLocalizer.Get("Vm_Status_ScanSourceFirst");
                return;
            }

            var selectedGroups = Groups.Where(g => g.IsSelected).ToList();
            if (selectedGroups.Count == 0)
            {
                StatusMessage = AppLocalizer.Get("Vm_Status_SelectAtLeastOneGroup");
                return;
            }

            try
            {
                ShowScanProgressDialog = true;
                ScanDialogTitle = AppLocalizer.Get("Vm_Scan_BuildingPreviewsDialogTitle");
                ScanProgressPercent = 0;
                ScannedFolders = 0;
                TotalFoldersToScan = selectedGroups.Count;
                ScannedFiles = 0;
                TotalFilesToScan = selectedGroups.SelectMany(g => g.Items).Count();
                ScanProgressMessage = AppLocalizer.Format("Vm_Scan_BuildingPreviewsForFolders", selectedGroups.Count);
                StatusMessage = AppLocalizer.Get("Vm_Status_BuildingPreviews");

                string sourceLabel = SelectedSource is FtpSourceItem ftpSource
                    ? $"{ftpSource.Host}{NormalizeFtpPath(ftpSource.RemoteFolder)}"
                    : SelectedSource.ToString() ?? "source";

                await LoadThumbnailsAsync(selectedGroups, SelectedSource, sourceLabel);
                ScannedFolders = TotalFoldersToScan;
            }
            catch (Exception ex)
            {
                StatusMessage = AppLocalizer.Format("Vm_Status_PreviewBuildFailed", ex.Message);
            }
            finally
            {
                ShowScanProgressDialog = false;
            }
        }
    }
}
