using CloudFlash.Core.Entities;
using CloudFlash.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text;

namespace CloudFlash.Infrastructure.Services;

public class TmdbStreamingService : IExternalStreamingService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl = "https://api.themoviedb.org/3";

    public TmdbStreamingService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["TmdbApiKey"] ?? throw new ArgumentNullException("TmdbApiKey configuration is required");
    }

    public async Task<IEnumerable<Title>> SearchTitlesAsync(string query, CancellationToken cancellationToken = default)
    {
        var titles = new List<Title>();

        // Buscar filmes
        var movieUrl = $"{_baseUrl}/search/movie?api_key={_apiKey}&query={Uri.EscapeDataString(query)}&language=pt-BR";
        var movieResponse = await _httpClient.GetStringAsync(movieUrl, cancellationToken);
        var movieData = JsonConvert.DeserializeObject<TmdbSearchResponse>(movieResponse);

        if (movieData?.Results != null)
        {
            titles.AddRange(movieData.Results.Select(x => MapToTitle(x)));
        }

        // Buscar séries
        var tvUrl = $"{_baseUrl}/search/tv?api_key={_apiKey}&query={Uri.EscapeDataString(query)}&language=pt-BR";
        var tvResponse = await _httpClient.GetStringAsync(tvUrl, cancellationToken);
        var tvData = JsonConvert.DeserializeObject<TmdbSearchResponse>(tvResponse);

        if (tvData?.Results != null)
        {
            titles.AddRange(tvData.Results.Select(result => MapToTitle(result, true)));
        }

        return titles;
    }

    public async Task<Title?> GetTitleDetailsAsync(string externalId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_baseUrl}/movie/{externalId}?api_key={_apiKey}&language=pt-BR";
            var response = await _httpClient.GetStringAsync(url, cancellationToken);
            var data = JsonConvert.DeserializeObject<TmdbMovieDetail>(response);

            return data != null ? MapDetailToTitle(data) : null;
        }
        catch
        {
            // Try as TV show if movie fails
            try
            {
                var url = $"{_baseUrl}/tv/{externalId}?api_key={_apiKey}&language=pt-BR";
                var response = await _httpClient.GetStringAsync(url, cancellationToken);
                var data = JsonConvert.DeserializeObject<TmdbTvDetail>(response);

                return data != null ? MapDetailToTitle(data) : null;
            }
            catch
            {
                return null;
            }
        }
    }

    public async Task<IEnumerable<StreamingAvailability>> GetStreamingAvailabilityAsync(string externalId, string region = "BR", CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_baseUrl}/movie/{externalId}/watch/providers?api_key={_apiKey}";
            var response = await _httpClient.GetStringAsync(url, cancellationToken);
            var data = JsonConvert.DeserializeObject<TmdbWatchProvidersResponse>(response);

            if (data?.Results?.ContainsKey(region) == true)
            {
                return MapToStreamingAvailability(data.Results[region], region);
            }
        }
        catch
        {
            // Try as TV show if movie fails
            try
            {
                var url = $"{_baseUrl}/tv/{externalId}/watch/providers?api_key={_apiKey}";
                var response = await _httpClient.GetStringAsync(url, cancellationToken);
                var data = JsonConvert.DeserializeObject<TmdbWatchProvidersResponse>(response);

                if (data?.Results?.ContainsKey(region) == true)
                {
                    return MapToStreamingAvailability(data.Results[region], region);
                }
            }
            catch
            {
                // Return empty if both fail
            }
        }

        return new List<StreamingAvailability>();
    }

    private Title MapToTitle(TmdbSearchResult result, bool isTvShow = false)
    {
        return new Title
        {
            Id = result.Id.ToString(),
            Name = isTvShow ? result.Name ?? result.Title ?? string.Empty : result.Title ?? result.Name ?? string.Empty,
            OriginalName = isTvShow ? result.OriginalName ?? result.OriginalTitle ?? string.Empty : result.OriginalTitle ?? result.OriginalName ?? string.Empty,
            Overview = result.Overview ?? string.Empty,
            PosterPath = !string.IsNullOrEmpty(result.PosterPath) ? $"https://image.tmdb.org/t/p/w500{result.PosterPath}" : string.Empty,
            BackdropPath = !string.IsNullOrEmpty(result.BackdropPath) ? $"https://image.tmdb.org/t/p/w1280{result.BackdropPath}" : string.Empty,
            ReleaseDate = DateTime.TryParse(isTvShow ? result.FirstAirDate : result.ReleaseDate, out var date) ? date : DateTime.MinValue,
            VoteAverage = result.VoteAverage,
            VoteCount = result.VoteCount,
            Type = isTvShow ? TitleType.TvShow : TitleType.Movie,
            GenreIds = result.GenreIds?.Select(g => g.ToString()).ToList() ?? new List<string>(),
            StreamingAvailabilities = new List<StreamingAvailability>()
        };
    }

    private Title MapDetailToTitle(TmdbMovieDetail detail)
    {
        return new Title
        {
            Id = detail.Id.ToString(),
            Name = detail.Title ?? string.Empty,
            OriginalName = detail.OriginalTitle ?? string.Empty,
            Overview = detail.Overview ?? string.Empty,
            PosterPath = !string.IsNullOrEmpty(detail.PosterPath) ? $"https://image.tmdb.org/t/p/w500{detail.PosterPath}" : string.Empty,
            BackdropPath = !string.IsNullOrEmpty(detail.BackdropPath) ? $"https://image.tmdb.org/t/p/w1280{detail.BackdropPath}" : string.Empty,
            ReleaseDate = DateTime.TryParse(detail.ReleaseDate, out var date) ? date : DateTime.MinValue,
            VoteAverage = detail.VoteAverage,
            VoteCount = detail.VoteCount,
            Type = TitleType.Movie,
            GenreIds = detail.Genres?.Select(g => g.Name).ToList() ?? new List<string>(),
            Runtime = detail.Runtime.ToString(),
            Status = detail.Status ?? string.Empty,
            ProductionCountries = detail.ProductionCountries?.Select(pc => pc.Name).ToList() ?? new List<string>(),
            StreamingAvailabilities = new List<StreamingAvailability>()
        };
    }

    private Title MapDetailToTitle(TmdbTvDetail detail)
    {
        return new Title
        {
            Id = detail.Id.ToString(),
            Name = detail.Name ?? string.Empty,
            OriginalName = detail.OriginalName ?? string.Empty,
            Overview = detail.Overview ?? string.Empty,
            PosterPath = !string.IsNullOrEmpty(detail.PosterPath) ? $"https://image.tmdb.org/t/p/w500{detail.PosterPath}" : string.Empty,
            BackdropPath = !string.IsNullOrEmpty(detail.BackdropPath) ? $"https://image.tmdb.org/t/p/w1280{detail.BackdropPath}" : string.Empty,
            ReleaseDate = DateTime.TryParse(detail.FirstAirDate, out var date) ? date : DateTime.MinValue,
            VoteAverage = detail.VoteAverage,
            VoteCount = detail.VoteCount,
            Type = TitleType.TvShow,
            GenreIds = detail.Genres?.Select(g => g.Name).ToList() ?? new List<string>(),
            Runtime = detail.EpisodeRunTime?.FirstOrDefault().ToString() ?? string.Empty,
            Status = detail.Status ?? string.Empty,
            ProductionCountries = detail.ProductionCountries?.Select(pc => pc.Name).ToList() ?? new List<string>(),
            StreamingAvailabilities = new List<StreamingAvailability>()
        };
    }

    private IEnumerable<StreamingAvailability> MapToStreamingAvailability(TmdbWatchProviders providers, string region)
    {
        var availabilities = new List<StreamingAvailability>();

        if (providers.Flatrate != null)
        {
            availabilities.AddRange(providers.Flatrate.Select(p => new StreamingAvailability
            {
                Platform = p.ProviderName,
                Region = region,
                Type = AvailabilityType.Subscription,
                Quality = "HD",
                Link = providers.Link ?? string.Empty
            }));
        }

        if (providers.Rent != null)
        {
            availabilities.AddRange(providers.Rent.Select(p => new StreamingAvailability
            {
                Platform = p.ProviderName,
                Region = region,
                Type = AvailabilityType.Rent,
                Quality = "HD",
                Link = providers.Link ?? string.Empty
            }));
        }

        if (providers.Buy != null)
        {
            availabilities.AddRange(providers.Buy.Select(p => new StreamingAvailability
            {
                Platform = p.ProviderName,
                Region = region,
                Type = AvailabilityType.Buy,
                Quality = "HD",
                Link = providers.Link ?? string.Empty
            }));
        }

        return availabilities;
    }
}

// DTOs para TMDB API
public class TmdbSearchResponse
{
    public int Page { get; set; }
    public List<TmdbSearchResult>? Results { get; set; }
    public int TotalPages { get; set; }
    public int TotalResults { get; set; }
}

public class TmdbSearchResult
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Name { get; set; }
    public string? OriginalTitle { get; set; }
    public string? OriginalName { get; set; }
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public string? ReleaseDate { get; set; }
    public string? FirstAirDate { get; set; }
    public double VoteAverage { get; set; }
    public int VoteCount { get; set; }
    public List<int>? GenreIds { get; set; }
}

public class TmdbMovieDetail
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? OriginalTitle { get; set; }
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public string? ReleaseDate { get; set; }
    public double VoteAverage { get; set; }
    public int VoteCount { get; set; }
    public int Runtime { get; set; }
    public string? Status { get; set; }
    public List<TmdbGenre>? Genres { get; set; }
    public List<TmdbProductionCountry>? ProductionCountries { get; set; }
}

public class TmdbTvDetail
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? OriginalName { get; set; }
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public string? FirstAirDate { get; set; }
    public double VoteAverage { get; set; }
    public int VoteCount { get; set; }
    public List<int>? EpisodeRunTime { get; set; }
    public string? Status { get; set; }
    public List<TmdbGenre>? Genres { get; set; }
    public List<TmdbProductionCountry>? ProductionCountries { get; set; }
}

public class TmdbGenre
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TmdbProductionCountry
{
    public string Iso31661 { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class TmdbWatchProvidersResponse
{
    public int Id { get; set; }
    public Dictionary<string, TmdbWatchProviders>? Results { get; set; }
}

public class TmdbWatchProviders
{
    public string? Link { get; set; }
    public List<TmdbProvider>? Flatrate { get; set; }
    public List<TmdbProvider>? Rent { get; set; }
    public List<TmdbProvider>? Buy { get; set; }
}

public class TmdbProvider
{
    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string LogoPath { get; set; } = string.Empty;
    public int DisplayPriority { get; set; }
}
