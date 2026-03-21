namespace QuickMediaIngest.Data.Models
{
    public record WhitelistRule(
        int Id = 0,
        string DeviceId = "",
        string Path = "",
        string RuleType = "Folder"
    );
}
