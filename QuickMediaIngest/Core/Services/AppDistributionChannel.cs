#nullable enable
using System;
using System.IO;

namespace QuickMediaIngest.Core.Services
{
    public enum DistributionChannel
    {
        Portable = 0,
        MsiInstaller = 1,
        Development = 2
    }

    public static class AppDistributionChannel
    {
        public static DistributionChannel DetectChannel()
        {
            try
            {
                string exePath = Environment.ProcessPath ?? AppDomain.CurrentDomain.BaseDirectory;
                string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

                if (!string.IsNullOrEmpty(progFiles) && exePath.StartsWith(progFiles, StringComparison.OrdinalIgnoreCase))
                {
                    return DistributionChannel.MsiInstaller;
                }

                if (!string.IsNullOrEmpty(progFilesX86) && exePath.StartsWith(progFilesX86, StringComparison.OrdinalIgnoreCase))
                {
                    return DistributionChannel.MsiInstaller;
                }

                if (exePath.Contains("bin\\Debug", StringComparison.OrdinalIgnoreCase) || exePath.Contains("bin/Debug", StringComparison.OrdinalIgnoreCase))
                {
                    return DistributionChannel.Development;
                }

                return DistributionChannel.Portable;
            }
            catch
            {
                return DistributionChannel.Portable;
            }
        }

        public static string GetChannelDisplay()
        {
            return DetectChannel() switch
            {
                DistributionChannel.MsiInstaller => "MSI Installer (Program Files)",
                DistributionChannel.Development => "Development Build",
                _ => "Portable Application"
            };
        }
    }
}
