#nullable enable
using System;

namespace QuickMediaIngest.Core.Services
{
    public enum ImportFreeSpaceDecision
    {
        Allow,
        /// <summary>Known selected bytes + margin exceed free space — abort.</summary>
        AbortInsufficient,
        /// <summary>Selected byte sum unknown (0) but free space is critically low — warn, user chooses.</summary>
        WarnUnknownSizesLowFree,
    }

    /// <summary>Pure free-space preflight decisions for import start.</summary>
    public static class ImportFreeSpaceGate
    {
        public const long MarginBytes = 256L * 1024 * 1024;
        public const long SoftWarnFreeBytes = 256L * 1024 * 1024;

        public static ImportFreeSpaceDecision Evaluate(long selectedBytes, long? freeBytes)
        {
            if (!freeBytes.HasValue)
            {
                return ImportFreeSpaceDecision.Allow;
            }

            long free = freeBytes.Value;
            if (selectedBytes > 0)
            {
                if (selectedBytes + MarginBytes > free)
                {
                    return ImportFreeSpaceDecision.AbortInsufficient;
                }

                return ImportFreeSpaceDecision.Allow;
            }

            if (free < SoftWarnFreeBytes)
            {
                return ImportFreeSpaceDecision.WarnUnknownSizesLowFree;
            }

            return ImportFreeSpaceDecision.Allow;
        }
    }
}
