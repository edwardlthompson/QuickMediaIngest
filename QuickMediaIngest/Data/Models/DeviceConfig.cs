namespace QuickMediaIngest.Data.Models
{
    public record DeviceConfig(
        string Id = "",
        string DeviceName = "",
        string LastImportDate = "",
        bool AutoTrigger = true
    );
}
