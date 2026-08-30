#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core.Services
{
    public static class ShootChecksumManifestWriter
    {
        public static async Task<string?> WriteManifestAsync(string shootDirectory, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(shootDirectory) || !Directory.Exists(shootDirectory))
            {
                return null;
            }

            try
            {
                var files = Directory.GetFiles(shootDirectory, "*.*", SearchOption.TopDirectoryOnly);
                var sb = new StringBuilder();
                sb.AppendLine($"# SHA-256 Manifest generated at {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");

                using var sha256 = SHA256.Create();
                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string fileName = Path.GetFileName(file);
                    if (fileName.Equals("checksums.sha256", StringComparison.OrdinalIgnoreCase) ||
                        fileName.Equals("manifest.sha256", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    using var stream = File.OpenRead(file);
                    byte[] hash = await sha256.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
                    string hashHex = Convert.ToHexString(hash).ToLowerInvariant();
                    sb.AppendLine($"{hashHex} *{fileName}");
                }

                string manifestPath = Path.Combine(shootDirectory, "checksums.sha256");
                await File.WriteAllTextAsync(manifestPath, sb.ToString(), Encoding.UTF8, cancellationToken).ConfigureAwait(false);
                return manifestPath;
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to write SHA-256 manifest for {ShootDir}", shootDirectory);
                return null;
            }
        }
    }
}
