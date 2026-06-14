#nullable enable

namespace QuickMediaIngest.Core.Services
{
    /// <summary>Resolves FTP passwords from in-memory values or Credential Manager.</summary>
    public static class FtpSourceCredentials
    {
        public static string ResolvePassword(
            string? pass,
            string host,
            int port,
            string? rawHost,
            IFtpCredentialStore store)
        {
            if (!string.IsNullOrEmpty(pass))
            {
                return pass;
            }

            string normalizedHost = FtpHostNormalizer.Normalize(host);
            return store.TryReadPasswordWithLegacyKeys(normalizedHost, port, rawHost, out string vaultPassword)
                ? vaultPassword
                : string.Empty;
        }
    }
}
