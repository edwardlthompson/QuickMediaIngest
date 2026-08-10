#nullable enable
using System;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Size-scaled wall-clock timeout for ADB pull. Floor 5 min, ceiling 10 min;
    /// concurrency + free-space preflight remain the primary hang mitigations.
    /// </summary>
    public static class AdbPullTimeout
    {
        public static readonly TimeSpan Floor = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan Ceiling = TimeSpan.FromMinutes(10);
        private static readonly long BytesPerSecondFloor = 1024L * 1024L; // 1 MB/s
        private static readonly TimeSpan Headroom = TimeSpan.FromMinutes(2);

        public static TimeSpan Compute(long expectedBytes)
        {
            if (expectedBytes <= 0)
            {
                return Floor;
            }

            double secondsAtFloor = (double)expectedBytes / BytesPerSecondFloor;
            TimeSpan scaled = TimeSpan.FromSeconds(secondsAtFloor) + Headroom;
            if (scaled < Floor)
            {
                return Floor;
            }

            if (scaled > Ceiling)
            {
                return Ceiling;
            }

            return scaled;
        }
    }
}
