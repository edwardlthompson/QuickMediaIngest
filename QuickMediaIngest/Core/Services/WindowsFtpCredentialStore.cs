#nullable enable
using System;
using Meziantou.Framework.Win32;

namespace QuickMediaIngest.Core.Services
{
    /// <summary>Persists FTP passwords via Windows Credential Manager using Meziantou.Framework.Win32.</summary>
    public sealed class WindowsFtpCredentialStore : IFtpCredentialStore
    {
        internal static string BuildTarget(string host, int port)
        {
            string h = (host ?? string.Empty).Trim();
            if (h.Contains('*') || h.Contains('\\'))
            {
                h = h.Replace('*', '_').Replace('\\', '/');
            }

            return $"QuickMediaIngest/FTP:{h}:{port}";
        }

        public bool TryReadPassword(string host, int port, out string password)
        {
            password = string.Empty;
            string target = BuildTarget(host, port);
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
