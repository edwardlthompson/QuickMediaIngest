#nullable enable
using System;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Caps PreferAdb / ADB pull concurrency. High parallelism stalls large pulls
    /// (especially against a nearly-full destination volume).
    /// </summary>
    public static class AdbTransferIo
    {
        public const int MaxConcurrentCopies = 2;

        /// <summary>
        /// Caps concurrent ADB file copies. When <paramref name="requested"/> is 0 (engine default),
        /// returns <see cref="MaxConcurrentCopies"/>.
        /// </summary>
        public static int CapConcurrentCopies(int requested)
        {
            if (requested <= 0)
            {
                return MaxConcurrentCopies;
            }

            return Math.Min(requested, MaxConcurrentCopies);
        }

        /// <summary>True when the provider performs ADB pulls (direct or remapped).</summary>
        public static bool IsAdbBackedProvider(IFileProvider? provider) =>
            provider is AdbFileProvider
            || (provider is RemappingFileProvider remapping && remapping.InnerIsAdb);
    }
}
