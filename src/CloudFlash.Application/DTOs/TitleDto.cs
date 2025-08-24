namespace CloudFlash.Application.DTOs;

public class TitleSearchDto
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
    public string Type { get; set; } = string.Empty;
    public List<string> Genres { get; set; } = new();
    public List<StreamingAvailabilityDto> StreamingAvailabilities { get; set; } = new();
    public string Runtime { get; set; } = string.Empty;
}

public class StreamingAvailabilityDto
{
    public string Platform { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Quality { get; set; } = string.Empty;
    public DateTime? AddedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Link { get; set; } = string.Empty;
}

public class TitleSearchRequestDto
{
    public string Query { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Genre { get; set; }
    public string? Platform { get; set; }
    public string? Type { get; set; }
    public string Region { get; set; } = "BR";
}

public class TitleSearchResponseDto
{
    public List<TitleSearchDto> Results { get; set; } = new();
    public int TotalResults { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
