#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core.Services
{
    public interface IImportHashCatalog
    {
        bool IsAlreadyImported(string hashHex);
        Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken = default);
        void RecordImported(string hashHex, string destinationPath);
        Task SaveAsync(string catalogPath);
        Task LoadAsync(string catalogPath);
    }

    public sealed class ImportHashCatalog : IImportHashCatalog
    {
        private readonly ConcurrentDictionary<string, string> _catalog = new(StringComparer.OrdinalIgnoreCase);
        private readonly ILogger<ImportHashCatalog>? _logger;

        public ImportHashCatalog(ILogger<ImportHashCatalog>? logger = null)
        {
            _logger = logger;
        }

        public bool IsAlreadyImported(string hashHex)
        {
            if (string.IsNullOrWhiteSpace(hashHex)) return false;
            return _catalog.ContainsKey(hashHex);
        }

        public async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath)) return string.Empty;
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            byte[] hash = await sha256.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public void RecordImported(string hashHex, string destinationPath)
        {
            if (!string.IsNullOrWhiteSpace(hashHex))
            {
                _catalog[hashHex] = destinationPath;
            }
        }

        public async Task SaveAsync(string catalogPath)
        {
            try
            {
                string? dir = Path.GetDirectoryName(catalogPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                string json = JsonSerializer.Serialize(_catalog, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(catalogPath, json).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to save import hash catalog to {Path}", catalogPath);
            }
        }

        public async Task LoadAsync(string catalogPath)
        {
            try
            {
                if (!File.Exists(catalogPath)) return;
                string json = await File.ReadAllTextAsync(catalogPath).ConfigureAwait(false);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict != null)
                {
                    foreach (var kvp in dict)
                    {
                        _catalog[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load import hash catalog from {Path}", catalogPath);
            }
        }
    }
}
