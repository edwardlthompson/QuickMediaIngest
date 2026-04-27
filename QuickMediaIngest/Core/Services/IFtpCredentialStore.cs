#nullable enable

namespace QuickMediaIngest.Core.Services
{
    /// <summary>Stores the FTP account password in Windows Credential Manager (per user, not in config.json).</summary>
    public interface IFtpCredentialStore
    {
        /// <summary>Target key: host + port. Returns false if not found.</summary>
        bool TryReadPassword(string host, int port, out string password);

        void WritePassword(string host, int port, string userName, string password);

        /// <summary>Remove stored secret for this FTP endpoint (empty password).</summary>
        void DeletePassword(string host, int port);
    }
}
