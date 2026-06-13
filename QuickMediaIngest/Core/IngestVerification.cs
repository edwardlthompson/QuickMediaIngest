#nullable enable
using System;
using System.IO;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    public static class IngestVerification
    {
        public static bool IsPostImportVerifiedForDelete(
            ImportItem item,
            string destPath,
            IngestOptions options,
            ILogger logger,
            out string? verifyNote)
        {
            verifyNote = null;
            if (!File.Exists(destPath))
            {
                verifyNote = "Destination missing.";
                return false;
            }

            var destInfo = new FileInfo(destPath);

            if (item.IsFtpSource)
            {
                if (item.FileSize != destInfo.Length)
                {
                    verifyNote = $"FTP listing size {item.FileSize} bytes vs destination {destInfo.Length} bytes.";
                    return false;
                }

                if (options.VerificationMode == ImportVerificationMode.Strict)
                {
                    logger.LogDebug("Strict verification for FTP source uses size match only for {FileName}.", item.FileName);
                }

                return true;
            }

            if (File.Exists(item.SourcePath))
            {
                var srcInfo = new FileInfo(item.SourcePath);
                if (options.VerificationMode == ImportVerificationMode.Strict)
                {
                    bool ok = srcInfo.Length == destInfo.Length
                        && ComputeSHA256(item.SourcePath) == ComputeSHA256(destPath);
                    if (!ok)
                    {
                        verifyNote = "Strict local verify failed (size or SHA-256 mismatch).";
                    }

                    return ok;
                }

                if (srcInfo.Length != destInfo.Length)
                {
                    verifyNote = $"Local source size {srcInfo.Length} vs destination {destInfo.Length}.";
                    return false;
                }

                return true;
            }

            if (item.FileSize != destInfo.Length)
            {
                verifyNote = $"Declared source size {item.FileSize} vs destination {destInfo.Length}.";
                return false;
            }

            return true;
        }

        public static string ComputeSHA256(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hash = sha.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }
    }
}
