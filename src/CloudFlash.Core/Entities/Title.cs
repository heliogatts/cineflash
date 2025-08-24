namespace CloudFlash.Core.Entities;

public class Title
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public string PosterPath { get; set; } = string.Empty;
    public string BackdropPath { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public double VoteAverage { get; set; }
    public int VoteCount { get; set; }
    public TitleType Type { get; set; }
    public List<string> GenreIds { get; set; } = new();
    public List<StreamingAvailability> StreamingAvailabilities { get; set; } = new();
    public List<string> ProductionCountries { get; set; } = new();
    public string Runtime { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum TitleType
{
    Movie,
    TvShow
}

public class StreamingAvailability
{
    public string Platform { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public AvailabilityType Type { get; set; }
    public decimal? Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Quality { get; set; } = string.Empty;
    public DateTime? AddedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Link { get; set; } = string.Empty;
}

public enum AvailabilityType
{
    Subscription,
    Rent,
    Buy,
    Free
}
