#nullable enable
using System;
using Meziantou.Framework.Win32;
using QuickMediaIngest.Core;

namespace QuickMediaIngest.Core.Services
{
    /// <summary>Persists FTP passwords via Windows Credential Manager using Meziantou.Framework.Win32.</summary>
    public sealed class WindowsFtpCredentialStore : IFtpCredentialStore
    {
        internal static string BuildTarget(string host, int port)
        {
            string h = FtpHostNormalizer.Normalize(host);
            if (h.Contains('*') || h.Contains('\\'))
            {
                h = h.Replace('*', '_').Replace('\\', '/');
            }

            return $"QuickMediaIngest/FTP:{h}:{port}";
        }

        private static string BuildLegacyTarget(string rawHost, int port)
        {
            string h = (rawHost ?? string.Empty).Trim();
            if (h.Contains('*') || h.Contains('\\'))
            {
                h = h.Replace('*', '_').Replace('\\', '/');
            }

            return $"QuickMediaIngest/FTP:{h}:{port}";
        }

        public bool TryReadPassword(string host, int port, out string password) =>
            TryReadPasswordForTarget(BuildTarget(host, port), out password);

        public bool TryReadPasswordWithLegacyKeys(string host, int port, string? rawHost, out string password)
        {
            if (TryReadPassword(host, port, out password))
            {
                return true;
            }

            string normalized = FtpHostNormalizer.Normalize(host);
            string? trimmedRaw = string.IsNullOrWhiteSpace(rawHost) ? null : rawHost.Trim();

            foreach (string candidate in GetLegacyHostCandidates(normalized, trimmedRaw))
            {
                if (TryReadPasswordForTarget(BuildLegacyTarget(candidate, port), out password))
                {
                    return true;
                }
            }

            password = string.Empty;
            return false;
        }

        private static System.Collections.Generic.IEnumerable<string> GetLegacyHostCandidates(string normalized, string? rawHost)
        {
            if (!string.IsNullOrWhiteSpace(rawHost))
            {
                yield return rawHost;
            }

            if (!string.IsNullOrWhiteSpace(normalized))
            {
                yield return $"ftp://{normalized}";
                yield return $"ftps://{normalized}";
            }
        }

        private static bool TryReadPasswordForTarget(string target, out string password)
        {
            password = string.Empty;
            try
            {
                var cred = CredentialManager.ReadCredential(target);
                if (cred == null || string.IsNullOrEmpty(cred.Password))
                {
                    return false;
                }

                password = cred.Password;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void WritePassword(string host, int port, string userName, string password)
        {
            string target = BuildTarget(host, port);
            CredentialManager.WriteCredential(
                applicationName: target,
                userName: string.IsNullOrWhiteSpace(userName) ? "FTP" : userName,
                secret: password ?? string.Empty,
                comment: "QuickMediaIngest FTP password",
                // CRED_PERSIST_LOCAL_MACHINE: persists across logons for this Windows user only
                // (not shared with other local accounts). Session would drop the password on logoff.
                persistence: CredentialPersistence.LocalMachine);
        }

        public void DeletePassword(string host, int port)
        {
            try
            {
                CredentialManager.DeleteCredential(BuildTarget(host, port));
            }
            catch
            {
                // Missing credential is acceptable.
            }
        }
    }
}
