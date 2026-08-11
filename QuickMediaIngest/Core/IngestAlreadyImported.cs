#nullable enable
using System;
using System.IO;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Recovers duplicate Prefer-ADB / re-import attempts when the destination
    /// already holds a verified copy (source may already be deleted).
    /// </summary>
    internal static class IngestAlreadyImported
    {
        public static bool TryFindVerifiedDestination(
            ImportItem item,
            string targetDir,
            string namingTemplate,
            string shootName,
            int sequenceNumber,
            IngestOptions options,
            ILogger logger,
            out string destPath)
        {
            destPath = string.Empty;
            if (string.IsNullOrWhiteSpace(targetDir))
            {
                return false;
            }

            string baseName = IngestFileNaming.BuildBaseFileName(
                item,
                namingTemplate,
                shootName,
                sequenceNumber);
            if (string.IsNullOrEmpty(baseName))
            {
                return false;
            }

            string ext = Path.GetExtension(baseName);
            string stem = Path.GetFileNameWithoutExtension(baseName);

            for (int counter = 0; counter <= 99; counter++)
            {
                string candidateName = counter == 0
                    ? baseName
                    : $"{stem}_{counter:D2}{ext}";
                string candidatePath = Path.Combine(targetDir, candidateName);
                if (!File.Exists(candidatePath))
                {
                    if (counter == 0)
                    {
                        continue;
                    }

                    break;
                }

                if (IngestVerification.IsPostImportVerifiedForDelete(
                        item,
                        candidatePath,
                        options,
                        logger,
                        out _))
                {
                    destPath = candidatePath;
                    return true;
                }
            }

            return false;
        }
    }
}
