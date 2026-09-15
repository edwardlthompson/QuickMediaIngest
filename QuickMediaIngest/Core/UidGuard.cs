#nullable enable
using System;
using System.Runtime.InteropServices;

namespace QuickMediaIngest.Core
{
    /// <summary>v1 Linux ingest-bench refuses to run as uid 0.</summary>
    public static class UidGuard
    {
        public static bool IsRoot()
        {
            if (OperatingSystem.IsWindows())
            {
                return false;
            }

            try
            {
                return getuid() == 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryRefuseRoot(out string message)
        {
            if (!IsRoot())
            {
                message = string.Empty;
                return true;
            }

            message = "Quick Media Ingest refuses to run as root (uid 0).";
            return false;
        }

        [DllImport("libc", SetLastError = true)]
        private static extern uint getuid();
    }
}
