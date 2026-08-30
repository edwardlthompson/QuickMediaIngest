#nullable enable
using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core.Services
{
    public sealed class ColorManagementProfileService
    {
        private readonly ILogger<ColorManagementProfileService>? _logger;

        public ColorManagementProfileService(ILogger<ColorManagementProfileService>? logger = null)
        {
            _logger = logger;
        }

        public string? GetSystemDefaultIccProfilePath()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    string colorDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "spool", "drivers", "color");
                    if (Directory.Exists(colorDir))
                    {
                        var srgb = Path.Combine(colorDir, "sRGB Color Space Profile.icm");
                        if (File.Exists(srgb)) return srgb;

                        var first = Directory.GetFiles(colorDir, "*.icm");
                        if (first.Length > 0) return first[0];
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Failed detecting system default ICC profile.");
            }

            return null;
        }
    }
}
