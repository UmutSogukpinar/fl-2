using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Caching;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.Models;
using FantasyLeague.Application.Services.NbaPlayers;
using Moq;

namespace FantasyLeague.Application.Tests.Services.NbaPlayers;

public sealed class NbaPlayerServiceAdditionalTests
{
    private readonly Mock<INbaPlayerRepository> _repository = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly NbaPlayerService _service;

    public NbaPlayerServiceAdditionalTests()
    {
        _service = new NbaPlayerService(_repository.Object, _cache.Object);
    }

    // Case: Get
    // Reasoning: This test verifies the Get operation.
    // Expected Result: The expected outcome is: Returns Correct Pagination Metadata.
    [Fact]
    public async Task GetAsync_ReturnsCorrectPaginationMetadata()
    {
        var request = new PaginationRequest { PageNumber = 3, PageSize = 5 };
        var players = new[]
        {
            new NbaPlayerBasicResponse(
                Guid.NewGuid(), "LeBron", "James", "LAL", "F")
        };
        _repository
            .Setup(repository => repository.GetPagedAsync(
                It.Is<PaginationRequest>(pagination =>
                    pagination.PageNumber == 3 && pagination.PageSize == 5),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((players, 11));

        var response = await _service.GetAsync(request);

        Assert.Same(players, response.Items);
        Assert.Equal(3, response.PageNumber);
        Assert.Equal(5, response.PageSize);
        Assert.Equal(11, response.TotalCount);
        Assert.Equal(3, response.TotalPages);
    }

    // Case: Get Nba Player By Id And Year when With Invalid Size
    // Reasoning: This test verifies Get Nba Player By Id And Year under the With Invalid Size condition.
    // Expected Result: The expected outcome is: Throws Bad Request.
    [Fact]
    public async Task GetNbaPlayerByIdAndYearAsync_WithInvalidSize_ThrowsBadRequest()
    {
        var invalidSize = (PlayerResponseSize)999;

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.GetNbaPlayerByIdAndYearAsync(
                Guid.NewGuid(), 2024, invalidSize));

        _cache.Verify(cache => cache.GetOrCreateAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<IPlayerResponse>>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(repository => repository.GetByIdAndSeasonAsync(
            It.IsAny<Guid>(),
            It.IsAny<int>(),
            It.IsAny<PlayerResponseSize>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
