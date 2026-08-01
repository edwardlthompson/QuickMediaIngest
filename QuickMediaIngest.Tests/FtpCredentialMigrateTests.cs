#nullable enable
using System;
using System.Collections.Generic;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class FtpCredentialMigrateTests
    {
        private sealed class MemoryFtpCredentialStore : IFtpCredentialStore
        {
            private readonly Dictionary<string, (string User, string Pass)> _map = new(StringComparer.OrdinalIgnoreCase);

            private static string Key(string host, int port) =>
                WindowsFtpCredentialStore.BuildTarget(host, port);

            public bool TryReadPassword(string host, int port, out string password)
            {
                if (_map.TryGetValue(Key(host, port), out var entry) && !string.IsNullOrEmpty(entry.Pass))
                {
                    password = entry.Pass;
                    return true;
                }

                password = string.Empty;
                return false;
            }

            public bool TryReadPasswordWithLegacyKeys(string host, int port, string? rawHost, out string password) =>
                TryReadPassword(host, port, out password);

            public void WritePassword(string host, int port, string userName, string password) =>
                _map[Key(host, port)] = (userName, password ?? string.Empty);

            public bool TryMigratePassword(string oldHost, string newHost, int port, string userName)
            {
                string oldNormalized = QuickMediaIngest.Core.FtpHostNormalizer.Normalize(oldHost);
                string newNormalized = QuickMediaIngest.Core.FtpHostNormalizer.Normalize(newHost);
                if (string.IsNullOrWhiteSpace(oldNormalized) ||
                    string.IsNullOrWhiteSpace(newNormalized) ||
                    string.Equals(oldNormalized, newNormalized, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (TryReadPassword(newNormalized, port, out _))
                {
                    return true;
                }

                if (!TryReadPasswordWithLegacyKeys(oldNormalized, port, oldHost, out string password) ||
                    string.IsNullOrEmpty(password))
                {
                    return false;
                }

                WritePassword(newNormalized, port, userName, password);
                return true;
            }

            public void DeletePassword(string host, int port) => _map.Remove(Key(host, port));
        }

        [Fact]
        public void TryMigratePassword_CopiesSecretToNewHost()
        {
            var store = new MemoryFtpCredentialStore();
            store.WritePassword("10.0.0.23", 2221, "android", "secret");

            Assert.True(store.TryMigratePassword("10.0.0.23", "10.0.0.7", 2221, "android"));
            Assert.True(store.TryReadPassword("10.0.0.7", 2221, out string migrated));
            Assert.Equal("secret", migrated);
            Assert.True(store.TryReadPassword("10.0.0.23", 2221, out string original));
            Assert.Equal("secret", original);
        }

        [Fact]
        public void TryMigratePassword_ReturnsFalseWhenOldMissing()
        {
            var store = new MemoryFtpCredentialStore();
            Assert.False(store.TryMigratePassword("10.0.0.23", "10.0.0.7", 2221, "android"));
        }

        [Fact]
        public void WindowsStore_TryMigratePassword_SameHost_ReturnsFalse()
        {
            var store = new WindowsFtpCredentialStore();
            Assert.False(store.TryMigratePassword("10.0.0.7", "10.0.0.7", 2221, "android"));
        }
    }
}
