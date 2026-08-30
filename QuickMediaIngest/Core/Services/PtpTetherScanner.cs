#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.Services
{
    public interface IPtpTetherScanner
    {
        bool IsSupportedOnPlatform { get; }
        Task<IReadOnlyList<ImportItem>> ScanPtpDeviceAsync(string deviceId, CancellationToken cancellationToken = default);
    }

    public sealed class PtpTetherScanner : IPtpTetherScanner
    {
        private readonly ILogger<PtpTetherScanner>? _logger;

        public bool IsSupportedOnPlatform => OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();

        public PtpTetherScanner(ILogger<PtpTetherScanner>? logger = null)
        {
            _logger = logger;
        }

        public async Task<IReadOnlyList<ImportItem>> ScanPtpDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            var items = new List<ImportItem>();
            _logger?.LogInformation("Scanning PTP tether media for {DeviceId}", deviceId);
            return items;
        }
    }
}
