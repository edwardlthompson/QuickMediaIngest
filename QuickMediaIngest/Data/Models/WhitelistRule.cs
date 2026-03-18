namespace QuickMediaIngest.Data.Models
{
    public class WhitelistRule
    {
        public int Id { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty; // e.g., "/DCIM"
        public string RuleType { get; set; } = "Folder"; // Folder, Extension
    }
}
