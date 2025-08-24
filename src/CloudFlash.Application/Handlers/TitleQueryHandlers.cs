using AutoMapper;
using CloudFlash.Application.DTOs;
using CloudFlash.Application.Queries;
using CloudFlash.Core.Interfaces;
using MediatR;

namespace CloudFlash.Application.Handlers;

public class SearchTitlesQueryHandler : IRequestHandler<SearchTitlesQuery, TitleSearchResponseDto>
{
    private readonly ITitleRepository _titleRepository;
    private readonly ISearchService _searchService;
    private readonly IExternalStreamingService _externalStreamingService;
    private readonly IMapper _mapper;

    public SearchTitlesQueryHandler(
        ITitleRepository titleRepository,
        ISearchService searchService,
        IExternalStreamingService externalStreamingService,
        IMapper mapper)
    {
        _titleRepository = titleRepository;
        _searchService = searchService;
        _externalStreamingService = externalStreamingService;
        _mapper = mapper;
    }

    public async Task<TitleSearchResponseDto> Handle(SearchTitlesQuery request, CancellationToken cancellationToken)
    {
        // Primeiro, busca no índice local (Elasticsearch)
        var localResults = await _searchService.SearchAsync<Core.Entities.Title>(
            request.Query,
            "titles",
            request.Page,
            request.PageSize,
            cancellationToken);

        var localTitles = localResults.ToList();

        // Se não encontrou resultados suficientes localmente, busca em APIs externas
        if (localTitles.Count < request.PageSize)
        {
            var externalResults = await _externalStreamingService.SearchTitlesAsync(request.Query, cancellationToken);

            // Adiciona novos títulos ao repositório e ao índice
            foreach (var externalTitle in externalResults.Take(request.PageSize - localTitles.Count))
            {
                try
                {
                    var savedTitle = await _titleRepository.CreateAsync(externalTitle, cancellationToken);
                    await _searchService.IndexTitleAsync(savedTitle, "titles", cancellationToken);
                    localTitles.Add(savedTitle);
                }
                catch
                {
                    // Log error but continue processing
                }
            }
        }

        // Aplicar filtros se especificados
        if (!string.IsNullOrEmpty(request.Platform))
        {
            localTitles = localTitles.Where(t =>
                t.StreamingAvailabilities.Any(sa =>
                    sa.Platform.Equals(request.Platform, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        if (!string.IsNullOrEmpty(request.Genre))
        {
            localTitles = localTitles.Where(t =>
                t.GenreIds.Contains(request.Genre, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrEmpty(request.Type))
        {
            if (Enum.TryParse<Core.Entities.TitleType>(request.Type, true, out var titleType))
            {
                localTitles = localTitles.Where(t => t.Type == titleType).ToList();
            }
        }

        var titleDtos = _mapper.Map<List<TitleSearchDto>>(localTitles);

        return new TitleSearchResponseDto
        {
            Results = titleDtos,
            TotalResults = titleDtos.Count,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling(titleDtos.Count / (double)request.PageSize)
        };
    }
}

public class GetTitleByIdQueryHandler : IRequestHandler<GetTitleByIdQuery, TitleSearchDto?>
{
    private readonly ITitleRepository _titleRepository;
    private readonly IMapper _mapper;

    public GetTitleByIdQueryHandler(ITitleRepository titleRepository, IMapper mapper)
    {
        _titleRepository = titleRepository;
        _mapper = mapper;
    }

    public async Task<TitleSearchDto?> Handle(GetTitleByIdQuery request, CancellationToken cancellationToken)
    {
        var title = await _titleRepository.GetByIdAsync(request.Id, cancellationToken);
        return title != null ? _mapper.Map<TitleSearchDto>(title) : null;
    }
}
