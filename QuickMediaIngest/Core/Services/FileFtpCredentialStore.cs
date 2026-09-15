#nullable enable
using System;
using System.IO;
using System.Text;

namespace QuickMediaIngest.Core.Services
{
    /// <summary>Linux v1: FTP secrets as 0600 files under <see cref="IAppPaths"/> (libsecret is a follow-up).</summary>
    public sealed class FileFtpCredentialStore : IFtpCredentialStore
    {
        private readonly string _dir;

        public FileFtpCredentialStore(IAppPaths paths)
        {
            _dir = Path.Combine(paths.AppDataRoot, "ftp-secrets");
        }

        public bool TryReadPassword(string host, int port, out string password) =>
            TryReadFile(FileName(host, port), out password);

        public bool TryReadPasswordWithLegacyKeys(string host, int port, string? rawHost, out string password)
        {
            if (TryReadPassword(host, port, out password))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(rawHost) && TryReadFile(FileName(rawHost, port), out password))
            {
                return true;
            }

            password = string.Empty;
            return false;
        }

        public void WritePassword(string host, int port, string userName, string password)
        {
            _ = userName;
            Directory.CreateDirectory(_dir);
            string path = Path.Combine(_dir, FileName(host, port));
            File.WriteAllText(path, password ?? string.Empty, Encoding.UTF8);
            TryChmod600(path);
        }

        public bool TryMigratePassword(string oldHost, string newHost, int port, string userName)
        {
            if (!TryReadPassword(oldHost, port, out string password) || string.IsNullOrEmpty(password))
            {
                return false;
            }

            if (string.Equals(FtpHostNormalizer.Normalize(oldHost), FtpHostNormalizer.Normalize(newHost), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            WritePassword(newHost, port, userName, password);
            return true;
        }

        public void DeletePassword(string host, int port)
        {
            try
            {
                File.Delete(Path.Combine(_dir, FileName(host, port)));
            }
            catch
            {
                // Best effort
            }
        }

        internal static string FileName(string host, int port)
        {
            string h = FtpHostNormalizer.Normalize(host);
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                h = h.Replace(c, '_');
            }

            return $"{h}_{port}.secret";
        }

        private bool TryReadFile(string name, out string password)
        {
            password = string.Empty;
            try
            {
                string path = Path.Combine(_dir, name);
                if (!File.Exists(path))
                {
                    return false;
                }

                password = File.ReadAllText(path, Encoding.UTF8);
                return !string.IsNullOrEmpty(password);
            }
            catch
            {
                return false;
            }
        }

        internal static void TryChmod600(string path)
        {
            if (OperatingSystem.IsWindows())
            {
                return;
            }

            try
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            catch
            {
                // Windows CI / non-unix FS
            }
        }
    }
}
