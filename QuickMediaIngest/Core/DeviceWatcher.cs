#nullable enable
using System;
using System.IO;
using System.Management;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Watches for device (volume) connections and disconnections using WMI events.
    /// </summary>
    public class DeviceWatcher : IDeviceWatcher, IDisposable
    {
    private ManagementEventWatcher? _watcher;
    private ManagementEventWatcher? _watcherDelete;
        private readonly ILogger<DeviceWatcher> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceWatcher"/> class.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output.</param>
        public DeviceWatcher(ILogger<DeviceWatcher> logger)
        {
            _logger = logger;
        }

        // Custom Events passing the Drive Letter (e.g. "E:\")
        /// <summary>
        /// Occurs when a device (volume) is connected.
        /// </summary>
        public event Action<string>? DeviceConnected;
        /// <summary>
        /// Occurs when a device (volume) is disconnected.
        /// </summary>
        public event Action<string>? DeviceDisconnected;

        /// <summary>
        /// Starts watching for device connection and disconnection events.
        /// </summary>
        public void Start()
        {
            try
            {
                _logger.LogInformation("Starting device watcher.");
                // Monitor for volume mountings
                string query = "SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_Volume'";
                _watcher = new ManagementEventWatcher(new WqlEventQuery(query));
                _watcher.EventArrived += Watcher_EventArrived;
                _watcher.Start();

                // Monitor for volume removals
                string queryDelete = "SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_Volume'";
                _watcherDelete = new ManagementEventWatcher(new WqlEventQuery(queryDelete));
                _watcherDelete.EventArrived += WatcherDelete_EventArrived;
                _watcherDelete.Start();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Device watcher failed to start.");
            }
        }

        /// <summary>
        /// Stops watching for device events and releases resources.
        /// </summary>
        public void Stop()
        {
            _logger.LogInformation("Stopping device watcher.");
            _watcher?.Stop();
            _watcher?.Dispose();
            _watcherDelete?.Stop();
            _watcherDelete?.Dispose();
        }

        /// <summary>
        /// Disposes the watcher and stops monitoring.
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        private void Watcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            var volume = e.NewEvent["TargetInstance"] as ManagementBaseObject;
            if (volume != null)
            {
                string? driveLetter = volume["DriveLetter"]?.ToString();
                if (!string.IsNullOrEmpty(driveLetter))
                {
                    if (!driveLetter.EndsWith("\\")) driveLetter += "\\";
                    _logger.LogInformation("Device connected: {DriveLetter}", driveLetter);
                    DeviceConnected?.Invoke(driveLetter);
                }
            }
        }

        private void WatcherDelete_EventArrived(object sender, EventArrivedEventArgs e)
        {
            var volume = e.NewEvent["TargetInstance"] as ManagementBaseObject;
            if (volume != null)
            {
                string? driveLetter = volume["DriveLetter"]?.ToString();
                if (!string.IsNullOrEmpty(driveLetter))
                {
                    if (!driveLetter.EndsWith("\\")) driveLetter += "\\";
                    _logger.LogInformation("Device disconnected: {DriveLetter}", driveLetter);
                    DeviceDisconnected?.Invoke(driveLetter);
                }
            }
        }
    }
}