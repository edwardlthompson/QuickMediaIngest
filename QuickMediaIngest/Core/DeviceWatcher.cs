using System;
using System.IO;
using System.Management;

namespace QuickMediaIngest.Core
{
    public class DeviceWatcher
    {
                private ManagementEventWatcher _watcher;
        private ManagementEventWatcher _watcherDelete;

        // Custom Events passing the Drive Letter (e.g. "E:\")
        public event Action<string> DeviceConnected; 
        public event Action<string> DeviceDisconnected; 

        public void Start()
        {
            try
            {
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
                Console.WriteLine($"[Watcher Error] {ex.Message}");
            }
        }

        public void Stop()
        {
            _watcher?.Stop();
            _watcher?.Dispose();
            _watcherDelete?.Stop();
            _watcherDelete?.Dispose();
        }

        private void Watcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            var volume = e.NewEvent["TargetInstance"] as ManagementBaseObject;
            if (volume != null)
            {
                string driveLetter = volume["DriveLetter"]?.ToString();
                if (!string.IsNullOrEmpty(driveLetter))
                {
                    if (!driveLetter.EndsWith("\\")) driveLetter += "\\";
                    DeviceConnected?.Invoke(driveLetter);
                }
            }
        }

        private void WatcherDelete_EventArrived(object sender, EventArrivedEventArgs e)
        {
            var volume = e.NewEvent["TargetInstance"] as ManagementBaseObject;
            if (volume != null)
            {
                string driveLetter = volume["DriveLetter"]?.ToString();
                if (!string.IsNullOrEmpty(driveLetter))
                {
                    if (!driveLetter.EndsWith("\\")) driveLetter += "\\";
                    DeviceDisconnected?.Invoke(driveLetter);
                }
            }
        }
        }
    }
}
