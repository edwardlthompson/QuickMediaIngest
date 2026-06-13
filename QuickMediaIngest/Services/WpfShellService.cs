#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Services
{
    public sealed class WpfShellService : IShellService
    {
        private readonly ILogger<WpfShellService> _logger;

        public WpfShellService(ILogger<WpfShellService> logger)
        {
            _logger = logger;
        }

        public void OpenFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
        }

        public void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}
