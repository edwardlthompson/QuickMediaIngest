using System;
using System.IO;
using System.Management;

namespace QuickMediaIngest.Core
{
    public class DeviceWatcher
    {
        private ManagementEventWatcher _watcher;

        // Custom Event passing the Drive Letter (e.g. "E:\")
        public event Action<string> DeviceConnected; 

        public void Start()
        {
            try
            {
                // Monitor for volume mountings (SD cards / USB sticks)
                string query = "SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_Volume'";
                _watcher = new ManagementEventWatcher(new WqlEventQuery(query));
                _watcher.EventArrived += Watcher_EventArrived;
                _watcher.Start();
            }
            catch (Exception ex)
            {
                // Fallback or log error
                Console.WriteLine($"[Watcher Error] {ex.Message}");
            }
        }

        public void Stop()
        {
            _watcher?.Stop();
            _watcher?.Dispose();
        }

        private void Watcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            var volume = e.NewEvent["TargetInstance"] as ManagementBaseObject;
            if (volume != null)
            {
                string driveLetter = volume["DriveLetter"]?.ToString();
                if (!string.IsNullOrEmpty(driveLetter))
                {
                    // Ensure standard formatting "X:\"
                    if (!driveLetter.EndsWith("\\")) 
                    {
                        driveLetter += "\\";
                    }
                    
                    DeviceConnected?.Invoke(driveLetter);
                }
            }
        }
    }
}
