#nullable enable
using System;
using System.Diagnostics;

namespace QuickMediaIngest.Core.Services
{
    /// <summary>Linux: Secret Service via <c>secret-tool</c>, then 0600 files (v1 always has the file fallback).</summary>
    public sealed class SecretServiceFtpCredentialStore : IFtpCredentialStore
    {
        private readonly FileFtpCredentialStore _files;

        public SecretServiceFtpCredentialStore(IAppPaths paths)
        {
            _files = new FileFtpCredentialStore(paths);
        }

        public bool TryReadPassword(string host, int port, out string password)
        {
            if (TrySecret("lookup", host, port, userName: "", secret: null, out password))
            {
                return true;
            }

            return _files.TryReadPassword(host, port, out password);
        }

        public bool TryReadPasswordWithLegacyKeys(string host, int port, string? rawHost, out string password) =>
            TryReadPassword(host, port, out password)
            || _files.TryReadPasswordWithLegacyKeys(host, port, rawHost, out password);

        public void WritePassword(string host, int port, string userName, string password)
        {
            _ = TrySecret("store", host, port, userName, password, out _);
            _files.WritePassword(host, port, userName, password);
        }

        public bool TryMigratePassword(string oldHost, string newHost, int port, string userName)
        {
            bool migrated = _files.TryMigratePassword(oldHost, newHost, port, userName);
            if (migrated)
            {
                if (_files.TryReadPassword(newHost, port, out string password))
                {
                    _ = TrySecret("store", newHost, port, userName, password, out _);
                }
            }

            return migrated;
        }

        public void DeletePassword(string host, int port)
        {
            _ = TrySecret("clear", host, port, userName: "", secret: null, out _);
            _files.DeletePassword(host, port);
        }

        internal static bool TrySecret(
            string verb,
            string host,
            int port,
            string userName,
            string? secret,
            out string password)
        {
            password = string.Empty;
            if (!OperatingSystem.IsLinux())
            {
                return false;
            }

            try
            {
                using var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "secret-tool",
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    },
                };
                proc.StartInfo.ArgumentList.Add(verb);
                if (verb == "store")
                {
                    proc.StartInfo.ArgumentList.Add("--label=Quick Media Ingest FTP");
                }

                proc.StartInfo.ArgumentList.Add("service");
                proc.StartInfo.ArgumentList.Add("quick-media-ingest");
                proc.StartInfo.ArgumentList.Add("host");
                proc.StartInfo.ArgumentList.Add(FtpHostNormalizer.Normalize(host));
                proc.StartInfo.ArgumentList.Add("port");
                proc.StartInfo.ArgumentList.Add(port.ToString());
                if (!proc.Start())
                {
                    return false;
                }

                if (verb == "store" && secret != null)
                {
                    proc.StandardInput.Write(secret);
                    proc.StandardInput.Close();
                }

                if (!proc.WaitForExit(1500))
                {
                    try { proc.Kill(); } catch { /* ignore */ }
                    return false;
                }

                if (proc.ExitCode != 0)
                {
                    return false;
                }

                if (verb == "lookup")
                {
                    password = proc.StandardOutput.ReadToEnd().Trim();
                    return password.Length > 0;
                }

                return verb is "store" or "clear";
            }
            catch
            {
                return false;
            }
        }
    }
}
