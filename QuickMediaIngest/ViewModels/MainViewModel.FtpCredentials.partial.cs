#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Services;
using QuickMediaIngest.Localization;

namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private string ResolveFtpPassword()
        {
            TryMigrateFtpVaultPasswordIfNeeded();
            return FtpSourceCredentials.ResolvePassword(FtpPass, FtpHost, FtpPort, FtpHost, _ftpCredentialStore);
        }

        private string ResolveFtpPasswordForSource(FtpSourceItem ftp) =>
            FtpSourceCredentials.ResolvePassword(ftp.Pass, ftp.Host, ftp.Port, ftp.Host, _ftpCredentialStore);

        private void RememberFtpVaultHost(string? host)
        {
            string normalized = FtpHostNormalizer.Normalize(host ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                _knownFtpVaultHosts.Add(normalized);
            }
        }

        private void TryMigrateFtpVaultPasswordIfNeeded()
        {
            if (string.IsNullOrWhiteSpace(FtpHost) || FtpPort <= 0)
            {
                return;
            }

            string newHost = FtpHostNormalizer.Normalize(FtpHost);
            RememberFtpVaultHost(newHost);

            if (_ftpCredentialStore.TryReadPassword(newHost, FtpPort, out string existing) &&
                !string.IsNullOrEmpty(existing))
            {
                return;
            }

            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(_previousFtpHost))
            {
                candidates.Add(_previousFtpHost);
            }

            candidates.AddRange(_knownFtpVaultHosts);
            foreach (var sidebar in Sources.OfType<FtpSourceItem>())
            {
                candidates.Add(sidebar.Host);
            }

            foreach (string oldHost in candidates
                         .Select(h => FtpHostNormalizer.Normalize(h))
                         .Where(h => !string.IsNullOrWhiteSpace(h))
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.Equals(oldHost, newHost, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (_ftpCredentialStore.TryMigratePassword(oldHost, newHost, FtpPort, FtpUser))
                {
                    _logger.LogInformation(
                        "Migrated FTP vault credential from {OldHost} to {NewHost}:{Port}.",
                        oldHost,
                        newHost,
                        FtpPort);
                    RememberFtpVaultHost(oldHost);
                    return;
                }
            }
        }

        private void EnsureFtpSourceCredentials(FtpSourceItem ftp)
        {
            ftp.Pass = ResolveFtpPasswordForSource(ftp);
        }

        private FtpEndpoint ToFtpEndpoint(FtpSourceItem ftp)
        {
            EnsureFtpSourceCredentials(ftp);
            string pass = ResolveFtpPasswordForSource(ftp);
            ftp.Pass = pass;
            return new FtpEndpoint(ftp.Host, ftp.Port, ftp.User, pass);
        }

        private async Task TryReconnectLastFtpAsync()
        {
            if (!AutoReconnectLastFtp || string.IsNullOrWhiteSpace(FtpHost))
            {
                return;
            }

            string remotePath = NormalizeFtpPath(string.IsNullOrWhiteSpace(FtpRemoteFolder) ? "/DCIM" : FtpRemoteFolder);
            RememberFtpVaultHost(FtpHost);
            string password = ResolveFtpPassword();
            try
            {
                if (string.IsNullOrEmpty(password))
                {
                    HasLastFtpReconnectFailure = true;
                    RefreshUxEmptyStateHints();
                    StatusMessage = AppLocalizer.Format("Vm_Ftp_PasswordMissingForHost", FtpHost, FtpPort);
                    _logger.LogWarning("FTP auto-reconnect skipped: no password for {Host}:{Port}.", FtpHost, FtpPort);
                    return;
                }

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                var result = await _ftpWorkflowService.TestConnectionAsync(
                    FtpHost, FtpPort, FtpUser, password, remotePath, 8, timeout.Token);

                if (!result.Success)
                {
                    HasLastFtpReconnectFailure = true;
                    RefreshUxEmptyStateHints();
                    StatusMessage = AppLocalizer.Format("Vm_Ftp_LastSourceUnreachable", FtpHost, FtpPort, remotePath);
                    _logger.LogWarning(
                        "FTP auto-reconnect failed for {Host}:{Port}{RemotePath}: {Message}",
                        FtpHost, FtpPort, remotePath, result.Message);
                    return;
                }

                HasLastFtpReconnectFailure = false;
                RefreshUxEmptyStateHints();
                FtpPermanentFailureCache.ClearEndpoint(FtpHost, FtpPort);
                FtpAdbAliasFilter.ClearSessionCache();

                var ftp = new FtpSourceItem
                {
                    Host = FtpHostNormalizer.Normalize(FtpHost),
                    Port = FtpPort,
                    User = FtpUser,
                    Pass = password,
                    RemoteFolder = remotePath
                };

                bool exists = Sources.OfType<FtpSourceItem>().Any(s =>
                    string.Equals(s.Host, ftp.Host, StringComparison.OrdinalIgnoreCase) &&
                    s.Port == ftp.Port &&
                    string.Equals(NormalizeFtpPath(s.RemoteFolder), remotePath, StringComparison.OrdinalIgnoreCase));

                if (!exists)
                {
                    Sources.Add(ftp);
                }
                else
                {
                    var existing = Sources.OfType<FtpSourceItem>().First(s =>
                        string.Equals(s.Host, ftp.Host, StringComparison.OrdinalIgnoreCase) &&
                        s.Port == ftp.Port &&
                        string.Equals(NormalizeFtpPath(s.RemoteFolder), remotePath, StringComparison.OrdinalIgnoreCase));
                    existing.Pass = password;
                }

                if (!Sources.Contains(_unifiedSource))
                {
                    Sources.Insert(0, _unifiedSource);
                }

                // Auto-browse after reconnect (triggers Unified load → PreferAdb scan/thumbs).
                SelectedSource = _unifiedSource;

                StatusMessage = AppLocalizer.Format("Vm_Ftp_Reconnected", FtpHost, FtpPort, remotePath);
                RememberFtpVaultHost(FtpHost);
                SaveConfig();
                _logger.LogInformation(
                    "FTP auto-reconnect succeeded for {Host}:{Port}{RemotePath}.",
                    FtpHost, FtpPort, remotePath);
            }
            catch (Exception ex)
            {
                HasLastFtpReconnectFailure = true;
                RefreshUxEmptyStateHints();
                StatusMessage = AppLocalizer.Format("Vm_Ftp_LastSourceUnreachable", FtpHost, FtpPort, remotePath);
                _logger.LogWarning(ex, "FTP auto-reconnect threw for {Host}:{Port}{RemotePath}.", FtpHost, FtpPort, remotePath);
            }
        }
    }
}
