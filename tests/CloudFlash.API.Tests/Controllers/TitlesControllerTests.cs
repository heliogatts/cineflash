using CloudFlash.API.Controllers;
using CloudFlash.Application.DTOs;
using CloudFlash.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Moq;
using FluentAssertions;

namespace CloudFlash.API.Tests.Controllers;

/// <summary>
/// Unit tests for the TitlesController class.
/// Tests cover successful scenarios, error handling, validation, and edge cases.
/// </summary>
public class TitlesControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<TitlesController>> _loggerMock;
    private readonly TitlesController _controller;

    public TitlesControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<TitlesController>>();
        _controller = new TitlesController(_mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SearchTitles_WithValidQueryAndParameters_ReturnsOkResult()
    {
        // Arrange
        var request = new TitleSearchRequestDto
        {
            Query = "batman",
            Page = 1,
            PageSize = 20
        };

        var expectedResponse = new TitleSearchResponseDto
        {
            Results = new List<TitleSearchDto>
            {
                new TitleSearchDto
                {
                    Id = "1",
                    Name = "Batman Begins",
                    Type = "Movie",
                    VoteAverage = 8.2
                }
            },
            TotalResults = 1,
            Page = 1,
            PageSize = 20,
            TotalPages = 1
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SearchTitlesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SearchTitles(request);

        // Assert
        result.Should().BeOfType<ActionResult<TitleSearchResponseDto>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TitleSearchResponseDto>().Subject;

        response.Results.Should().HaveCount(1);
        response.Results.First().Name.Should().Be("Batman Begins");
        response.TotalResults.Should().Be(1);
        response.Page.Should().Be(1);
        response.PageSize.Should().Be(20);
        response.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetTitle_WithValidId_ReturnsOkResult()
    {
        // Arrange
        const string titleId = "123";
        var expectedTitle = new TitleSearchDto
        {
            Id = titleId,
            Name = "Batman Begins",
            Type = "Movie",
            VoteAverage = 8.2
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetTitleByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTitle);

        // Act
        var result = await _controller.GetTitle(titleId);

        // Assert
        result.Should().BeOfType<ActionResult<TitleSearchDto>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var title = okResult.Value.Should().BeOfType<TitleSearchDto>().Subject;

        title.Id.Should().Be(titleId);
        title.Name.Should().Be("Batman Begins");
        title.Type.Should().Be("Movie");
        title.VoteAverage.Should().Be(8.2);

        _mediatorMock.Verify(
            m => m.Send(It.Is<GetTitleByIdQuery>(q => q.Id == titleId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTitle_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        const string titleId = "999";

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetTitleByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TitleSearchDto?)null);

        // Act
        var result = await _controller.GetTitle(titleId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().Be($"Title with ID {titleId} not found");

        _mediatorMock.Verify(
            m => m.Send(It.Is<GetTitleByIdQuery>(q => q.Id == titleId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchTitles_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var request = new TitleSearchRequestDto
        {
            Query = "batman",
            Page = 1,
            PageSize = 20
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SearchTitlesQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.SearchTitles(request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(500);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchTitles_WithInvalidQuery_ReturnsBadRequest(string? query)
    {
        // Arrange
        var request = new TitleSearchRequestDto
        {
            Query = query!,
            Page = 1,
            PageSize = 20
        };

        // Act
        var result = await _controller.SearchTitles(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();

        _mediatorMock.Verify(
            m => m.Send(It.IsAny<SearchTitlesQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchTitles_WithLargePageSize_LimitsToMaximum()
    {
        // Arrange
        var request = new TitleSearchRequestDto
        {
            Query = "batman",
            Page = 1,
            PageSize = 100 // Should be limited to 50
        };

        var expectedResponse = new TitleSearchResponseDto
        {
            Results = new List<TitleSearchDto>(),
            TotalResults = 0,
            Page = 1,
            PageSize = 50,
            TotalPages = 0
        };

        _mediatorMock
            .Setup(m => m.Send(It.Is<SearchTitlesQuery>(q => q.PageSize == 50), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SearchTitles(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();

        _mediatorMock.Verify(
            m => m.Send(It.Is<SearchTitlesQuery>(q => q.PageSize == 50), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTitle_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        const string titleId = "123";

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetTitleByIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetTitle(titleId);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task SearchTitles_WithAllParameters_PassesCorrectQuery()
    {
        // Arrange
        var request = new TitleSearchRequestDto
        {
            Query = "batman",
            Page = 2,
            PageSize = 10,
            Genre = "Action",
            Platform = "Netflix",
            Type = "Movie",
            Region = "US"
        };

        var expectedResponse = new TitleSearchResponseDto
        {
            Results = new List<TitleSearchDto>(),
            TotalResults = 0,
            Page = 2,
            PageSize = 10,
            TotalPages = 0
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SearchTitlesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SearchTitles(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();

        _mediatorMock.Verify(
            m => m.Send(
                It.Is<SearchTitlesQuery>(q =>
                    q.Query == "batman" &&
                    q.Page == 2 &&
                    q.PageSize == 10 &&
                    q.Genre == "Action" &&
                    q.Platform == "Netflix" &&
                    q.Type == "Movie" &&
                    q.Region == "US"
                ),
                It.IsAny<CancellationToken>()
            ),
            Times.Once);
    }
}
