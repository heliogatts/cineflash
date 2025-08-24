using CloudFlash.Application.DTOs;
using MediatR;

namespace CloudFlash.Application.Queries;

public class SearchTitlesQuery : IRequest<TitleSearchResponseDto>
{
    public string Query { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Genre { get; set; }
    public string? Platform { get; set; }
    public string? Type { get; set; }
    public string Region { get; set; } = "BR";
}

public class GetTitleByIdQuery : IRequest<TitleSearchDto?>
{
    public string Id { get; set; } = string.Empty;
}
