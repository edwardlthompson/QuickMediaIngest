#nullable enable
using System;
using System.IO;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    public static class IngestFileNaming
    {
        public static string ResolveFileName(
            ImportItem item,
            string targetDir,
            string template,
            string shootName,
            int sequenceNumber,
            DuplicateHandlingMode duplicateHandling,
            out bool skippedAsDuplicate)
        {
            skippedAsDuplicate = false;
            string ext = Path.GetExtension(item.FileName);
            string outputName = template;
            string safeShootName = SanitizeFileNamePart(string.IsNullOrWhiteSpace(shootName) ? "Shoot" : shootName);
            DateTime effectiveDateTaken = item.DateTaken;
            if ((template.Contains("[fff]", StringComparison.Ordinal) || template.Contains("[TimeMs]", StringComparison.Ordinal))
                && effectiveDateTaken.Millisecond == 0)
            {
                int syntheticMs = Math.Clamp(sequenceNumber % 1000, 1, 999);
                effectiveDateTaken = new DateTime(
                    effectiveDateTaken.Year,
                    effectiveDateTaken.Month,
                    effectiveDateTaken.Day,
                    effectiveDateTaken.Hour,
                    effectiveDateTaken.Minute,
                    effectiveDateTaken.Second,
                    syntheticMs,
                    effectiveDateTaken.Kind);
            }

            if (string.IsNullOrEmpty(outputName))
            {
                outputName = "[Date]_[Time]_[Original]";
            }

            outputName = outputName.Replace("[Date]", effectiveDateTaken.ToString("yyyy-MM-dd"));
            outputName = outputName.Replace("[Time]", effectiveDateTaken.ToString("HH-mm-ss"));
            outputName = outputName.Replace("[TimeMs]", effectiveDateTaken.ToString("HH-mm-ss-fff"));
            outputName = outputName.Replace("[YYYY]", effectiveDateTaken.ToString("yyyy"));
            outputName = outputName.Replace("[MM]", effectiveDateTaken.ToString("MM"));
            outputName = outputName.Replace("[DD]", effectiveDateTaken.ToString("dd"));
            outputName = outputName.Replace("[HH]", effectiveDateTaken.ToString("HH"));
            outputName = outputName.Replace("[mm]", effectiveDateTaken.ToString("mm"));
            outputName = outputName.Replace("[ss]", effectiveDateTaken.ToString("ss"));
            outputName = outputName.Replace("[fff]", effectiveDateTaken.ToString("fff"));
            outputName = outputName.Replace("[ShootName]", safeShootName);
            outputName = outputName.Replace("[Original]", Path.GetFileNameWithoutExtension(item.FileName));
            outputName = outputName.Replace("[Sequence]", sequenceNumber.ToString("D4"));
            outputName = outputName.Replace("[Ext]", ext.TrimStart('.'));

            string destFileName = $"{outputName}{ext}";
            string fullPath = Path.Combine(targetDir, destFileName);

            if (File.Exists(fullPath))
            {
                switch (duplicateHandling)
                {
                    case DuplicateHandlingMode.Skip:
                        skippedAsDuplicate = true;
                        return string.Empty;
                    case DuplicateHandlingMode.OverwriteIfNewer:
                        try
                        {
                            var dstInfo = new FileInfo(fullPath);
                            if (item.IsFtpSource)
                            {
                                if (item.DateTaken.ToUniversalTime() <= dstInfo.LastWriteTimeUtc)
                                {
                                    skippedAsDuplicate = true;
                                    return string.Empty;
                                }
                            }
                            else
                            {
                                var srcInfo = new FileInfo(item.SourcePath);
                                if (srcInfo.LastWriteTimeUtc <= dstInfo.LastWriteTimeUtc)
                                {
                                    skippedAsDuplicate = true;
                                    return string.Empty;
                                }
                            }
                        }
                        catch
                        {
                            skippedAsDuplicate = true;
                            return string.Empty;
                        }

                        return destFileName;
                }
            }

            int counter = 1;
            while (File.Exists(fullPath))
            {
                destFileName = $"{outputName}_{counter:D2}{ext}";
                fullPath = Path.Combine(targetDir, destFileName);
                counter++;
            }

            return destFileName;
        }

        private static string SanitizeFileNamePart(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return value.Trim();
        }
    }
}
