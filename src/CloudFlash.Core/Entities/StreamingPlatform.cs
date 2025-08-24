namespace CloudFlash.Core.Entities;

public class StreamingPlatform
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string LogoPath { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public List<string> SupportedRegions { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
