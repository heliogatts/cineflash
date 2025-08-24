using CloudFlash.Application.DTOs;
using CloudFlash.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CloudFlash.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class TitlesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TitlesController> _logger;

    public TitlesController(IMediator mediator, ILogger<TitlesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Busca filmes e séries disponíveis em plataformas de streaming
    /// </summary>
    /// <param name="request">Parâmetros de busca</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de títulos encontrados</returns>
    [HttpGet]
    [ProducesResponseType(typeof(TitleSearchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TitleSearchResponseDto>> SearchTitles(
        [FromQuery] TitleSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest("Query parameter is required");
            }

            _logger.LogInformation("Searching titles with query: {Query}", request.Query);

            var query = new SearchTitlesQuery
            {
                Query = request.Query,
                Page = request.Page,
                PageSize = Math.Min(request.PageSize, 50), // Limita o tamanho da página
                Genre = request.Genre,
                Platform = request.Platform,
                Type = request.Type,
                Region = request.Region
            };

            var result = await _mediator.Send(query, cancellationToken);

            _logger.LogInformation("Found {Count} titles for query: {Query}", result.TotalResults, request.Query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching titles with query: {Query}", request.Query);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while searching titles");
        }
    }

    /// <summary>
    /// Obtém detalhes de um título específico
    /// </summary>
    /// <param name="id">ID do título</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Detalhes do título</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TitleSearchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TitleSearchDto>> GetTitle(
        string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting title with ID: {Id}", id);

            var query = new GetTitleByIdQuery { Id = id };
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
                _logger.LogWarning("Title not found with ID: {Id}", id);
                return NotFound($"Title with ID {id} not found");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting title with ID: {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the title");
        }
    }
}
