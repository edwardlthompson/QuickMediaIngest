namespace QuickMediaIngest.Data.Models
{
    public class DeviceConfig
    {
        public string Id { get; set; } = string.Empty; // GUID stored in .importer-id
        public string DeviceName { get; set; } = string.Empty;
        public string LastImportDate { get; set; } = string.Empty;
        public bool AutoTrigger { get; set; } = true;
    }
}
