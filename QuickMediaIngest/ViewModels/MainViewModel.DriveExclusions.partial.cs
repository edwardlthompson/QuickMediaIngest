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
    public partial class MainViewModel : ObservableObject
    {

        private async Task ExecuteRemoveDriveExclusionAsync(string? deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return;
            }

            if (_selectedDriveDeviceIds.Count == 0)
            {
                // Seed explicit selections from implicit defaults so removing a fixed-drive
                // exclusion does not accidentally exclude all removable drives.
                var removableDrives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady && d.DriveType == DriveType.Removable)
                    .ToList();
                foreach (var d in removableDrives)
                {
                    string id = await ResolveDeviceIdWithTimeoutAsync(d).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        _selectedDriveDeviceIds.Add(id);
                    }
                }
            }

            _selectedDriveDeviceIds.Add(deviceId);
            await ScanDrivesAsync();
            await RefreshExclusionManagementListsAsync().ConfigureAwait(true);
            SaveConfig();
        }

        private void ExecuteRemoveSkippedFolderRule(SkippedFolderRuleEntry? entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.SourceId) || string.IsNullOrWhiteSpace(entry.FolderPath))
            {
                return;
            }

            if (_skippedFoldersBySource.TryGetValue(entry.SourceId, out var list))
            {
                bool ftpRule = entry.SourceId.StartsWith("ftp|", StringComparison.OrdinalIgnoreCase);
                list.RemoveAll(p => FolderPathsMatchForSkipRule(p, entry.FolderPath, ftpRule));
                if (list.Count == 0)
                {
                    _skippedFoldersBySource.Remove(entry.SourceId);
                }

                _ = RefreshExclusionManagementListsAsync();
                SaveConfig();
                InvalidateSourceItemsCache(entry.SourceId);
            }
        }

        private static bool FolderPathsMatchForSkipRule(string a, string b, bool isFtp)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            {
                return false;
            }

            if (isFtp)
            {
                return string.Equals(NormalizeFtpPath(a), NormalizeFtpPath(b), StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(a.TrimEnd('\\', '/'), b.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
        }

        private void InvalidateSourceItemsCache(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                return;
            }

            _sourceItemsCache.Remove(sourceId);
        }

        /// <summary>
        /// Must run after <see cref="StampItems"/> so FTP items carry the same source key as skip rules.
        /// Local rules use local-device|… keys (see <see cref="BuildLocalSourceRuleKey"/>), not raw stamped keys.
        /// </summary>
        private void ApplySkippedFolderFilters(List<ImportItem> items, HashSet<string> userExcludedFolders)
        {
            int before = items.Count;
            items.RemoveAll(item =>
            {
                string ruleLookupKey = GetSkipRuleLookupKey(item);
                if (string.IsNullOrWhiteSpace(ruleLookupKey) ||
                    !_skippedFoldersBySource.TryGetValue(ruleLookupKey, out var skippedPrefixes) ||
                    skippedPrefixes.Count == 0)
                {
                    return false;
                }

                string folder = item.IsFtpSource
                    ? ExtractFtpFolderPath(item.SourcePath)
                    : (Path.GetDirectoryName(item.SourcePath) ?? string.Empty);

                bool shouldSkip = skippedPrefixes.Any(prefix =>
                {
                    if (string.IsNullOrWhiteSpace(prefix))
                    {
                        return false;
                    }

                    string normalizedPrefix = item.IsFtpSource ? NormalizeFtpPath(prefix) : prefix.TrimEnd('\\', '/');
                    string normalizedFolder = item.IsFtpSource ? NormalizeFtpPath(folder) : folder.TrimEnd('\\', '/');

                    return normalizedFolder.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase);
                });

                if (shouldSkip)
                {
                    userExcludedFolders.Add(folder);
                }

                return shouldSkip;
            });

            int removed = before - items.Count;
            if (removed > 0)
            {
                _logger.LogInformation(
                    "Skipped-folder blacklist removed {Removed} import item(s) from {Before} scanned (active rule sources: {RuleCount}).",
                    removed,
                    before,
                    _skippedFoldersBySource.Count);
            }
        }

        private string GetSkipRuleLookupKey(ImportItem item)
        {
            if (item.IsFtpSource)
            {
                return item.SourceId ?? string.Empty;
            }

            string root = Path.GetPathRoot(item.SourcePath) ?? string.Empty;
            return string.IsNullOrWhiteSpace(root) ? string.Empty : BuildLocalSourceRuleKey(root);
        }

        /// <summary>
        /// Rebuilds the skipped-folder blacklist UI from in-memory rules (same keys as ingest filtering).
        /// Kept separate from drive enumeration so the list stays accurate even when drive I/O fails.
        /// </summary>
        private void RebuildSkippedFolderRuleEntries()
        {
            SkippedFolderRuleEntries.Clear();
            foreach (var kvp in _skippedFoldersBySource.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                foreach (var folder in kvp.Value.OrderBy(v => v, StringComparer.OrdinalIgnoreCase))
                {
                    SkippedFolderRuleEntries.Add(new SkippedFolderRuleEntry
                    {
                        SourceId = kvp.Key,
                        FolderPath = folder
                    });
                }
            }
        }

        private async Task RefreshExclusionManagementListsAsync()
        {
            await Application.Current.Dispatcher.InvokeAsync(RebuildSkippedFolderRuleEntries).Task.ConfigureAwait(true);

            List<DriveInfo> drives;
            try
            {
                drives = await Task.Run(() =>
                    DriveInfo.GetDrives()
                        .Where(d => d.IsReady && (d.DriveType == DriveType.Removable || d.DriveType == DriveType.Fixed))
                        .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Drive enumeration failed for exclusion UI; skipped-folder blacklist was still refreshed.");
                await Application.Current.Dispatcher.InvokeAsync(() => ExcludedDriveEntries.Clear()).Task.ConfigureAwait(true);
                return;
            }

            (DriveInfo drive, string deviceId)[] resolved = await Task.WhenAll(
                drives.Select(async d =>
                {
                    string id = await ResolveDeviceIdWithTimeoutAsync(d).ConfigureAwait(false);
                    return (drive: d, deviceId: id);
                })).ConfigureAwait(false);

            HashSet<string> selectedSnapshot = await Application.Current.Dispatcher
                .InvokeAsync(() => new HashSet<string>(_selectedDriveDeviceIds, StringComparer.OrdinalIgnoreCase))
                .Task
                .ConfigureAwait(false);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var pair in resolved)
                {
                    _driveDeviceIdByPath[pair.drive.Name] = pair.deviceId;
                    _drivePathByDeviceId[pair.deviceId] = pair.drive.Name;
                }

                ExcludedDriveEntries.Clear();
                foreach (var pair in resolved)
                {
                    bool isExcluded = selectedSnapshot.Count == 0
                        ? pair.drive.DriveType == DriveType.Fixed
                        : !selectedSnapshot.Contains(pair.deviceId);
                    if (isExcluded)
                    {
                        ExcludedDriveEntries.Add(new ExcludedDriveEntry
                        {
                            DeviceId = pair.deviceId,
                            DriveName = pair.drive.Name,
                            DriveType = pair.drive.DriveType.ToString()
                        });
                    }
                }
            }).Task.ConfigureAwait(true);
        }

        private const int DeviceIdIoTimeoutMs = 2500;

        private static string GetDeviceIdMarkerPath(DriveInfo drive) => Path.Combine(drive.RootDirectory.FullName, ".quickmediaingest-device.id");

        private static string PathFallbackDeviceId(DriveInfo drive) => $"path:{drive.Name.ToUpperInvariant()}";

        /// <summary>
        /// Synchronous per-drive root I/O. Can block indefinitely on a bad volume; use only from
        /// <see cref="GetOrCreateDeviceIdForDrive"/> or <see cref="ResolveDeviceIdWithTimeoutAsync"/> runners.
        /// </summary>
        private static string TryReadOrWriteDeviceMarker(DriveInfo drive)
        {
            string markerPath = GetDeviceIdMarkerPath(drive);
            try
            {
                if (File.Exists(markerPath))
                {
                    string existing = File.ReadAllText(markerPath).Trim();
                    if (!string.IsNullOrWhiteSpace(existing))
                    {
                        return existing;
                    }
                }

                string deviceId = Guid.NewGuid().ToString("N");
                File.WriteAllText(markerPath, deviceId);
                return deviceId;
            }
            catch
            {
                return PathFallbackDeviceId(drive);
            }
        }

        private async Task<string> ResolveDeviceIdWithTimeoutAsync(DriveInfo drive)
        {
            try
            {
                var task = Task.Run(() => TryReadOrWriteDeviceMarker(drive));
                return await task.WaitAsync(TimeSpan.FromMilliseconds(DeviceIdIoTimeoutMs)).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Device id I/O timed out for {Drive} after {TimeoutMs} ms; using path fallback.", drive.Name, DeviceIdIoTimeoutMs);
                return PathFallbackDeviceId(drive);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Device id I/O failed for {Drive}; using path fallback.", drive.Name);
                return PathFallbackDeviceId(drive);
            }
        }

        private string GetOrCreateDeviceIdForDrive(DriveInfo drive)
        {
            try
            {
                var task = Task.Run(() => TryReadOrWriteDeviceMarker(drive));
                if (task.Wait(TimeSpan.FromMilliseconds(DeviceIdIoTimeoutMs)))
                {
                    return task.Result;
                }

                _logger.LogWarning("Device id I/O timed out for {Drive} after {TimeoutMs} ms; using path fallback.", drive.Name, DeviceIdIoTimeoutMs);
                return PathFallbackDeviceId(drive);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Device id I/O failed for {Drive}; using path fallback.", drive.Name);
                return PathFallbackDeviceId(drive);
            }
        }

        private string ResolveDeviceIdFromLocalPath(string localPath)
        {
            string root = Path.GetPathRoot(localPath) ?? localPath;
            if (_driveDeviceIdByPath.TryGetValue(root, out var known))
            {
                return known;
            }

            try
            {
                var info = new DriveInfo(root);
                if (info.IsReady)
                {
                    string id = GetOrCreateDeviceIdForDrive(info);
                    _driveDeviceIdByPath[info.Name] = id;
                    _drivePathByDeviceId[id] = info.Name;
                    return id;
                }
            }
            catch
            {
                // Fallback below.
            }

            return $"path:{root.ToUpperInvariant()}";
        }
    }
}
