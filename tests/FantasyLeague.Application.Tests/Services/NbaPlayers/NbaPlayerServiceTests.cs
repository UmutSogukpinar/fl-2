using FantasyLeague.Application.Common.Caching;
using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Caching;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Requests.NbaPlayers;
using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.Models;
using FantasyLeague.Application.Services.NbaPlayers;
using Moq;

namespace FantasyLeague.Application.Tests.Services.NbaPlayers;

public sealed class NbaPlayerServiceTests
{
    private readonly Mock<INbaPlayerRepository> _repository = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly NbaPlayerService _service;

    public NbaPlayerServiceTests()
    {
        _service = new NbaPlayerService(_repository.Object, _cache.Object);
    }

    // Case: Get
    // Reasoning: This test verifies the Get operation.
    // Expected Result: The expected outcome is: Returns Paged Players.
    [Fact]
    public async Task GetAsync_ReturnsPagedPlayers()
    {
        var player = CreateBasicResponse();
        var request = new PaginationRequest
        {
            PageNumber = 2,
            PageSize = 5
        };
        _repository
            .Setup(repository => repository.GetPagedAsync(
                It.Is<PaginationRequest>(pagination =>
                    pagination.PageNumber == 2 && pagination.PageSize == 5),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(([player], 12));

        var result = await _service.GetAsync(request);

        Assert.Same(player, Assert.Single(result.Items));
        Assert.Equal(12, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.PageNumber);
    }

    // Case: Get when Pagination Is Invalid
    // Reasoning: This test verifies Get under the Pagination Is Invalid condition.
    // Expected Result: The expected outcome is: Does Not Query Repository.
    [Fact]
    public async Task GetAsync_WhenPaginationIsInvalid_DoesNotQueryRepository()
    {
        var request = new PaginationRequest { PageNumber = 0 };

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.GetAsync(request));

        _repository.Verify(repository => repository.GetPagedAsync(
            It.IsAny<PaginationRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Case: Get Nba Player By Id And Year
    // Reasoning: This test verifies the Get Nba Player By Id And Year operation.
    // Expected Result: The expected outcome is: Uses Basic Cache Key.
    [Fact]
    public async Task GetNbaPlayerByIdAndYearAsync_UsesBasicCacheKey()
    {
        var player = CreateBasicResponse();
        SetupCacheFactory();
        _repository
            .Setup(repository => repository.GetByIdAndSeasonAsync(
                player.Id,
                2026,
                PlayerResponseSize.Basic,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(player);

        var result = await _service.GetNbaPlayerByIdAndYearAsync(
            player.Id,
            2026,
            PlayerResponseSize.Basic);

        Assert.Same(player, result);
        _cache.Verify(cache => cache.GetOrCreateAsync(
            CacheKeys.NbaPlayerBasic(player.Id),
            It.IsAny<Func<CancellationToken, Task<IPlayerResponse>>>(),
            TimeSpan.FromDays(1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Case: Get Nba Player By Id And Year when Player Is Missing
    // Reasoning: This test verifies Get Nba Player By Id And Year under the Player Is Missing condition.
    // Expected Result: The expected outcome is: Throws Not Found.
    [Fact]
    public async Task GetNbaPlayerByIdAndYearAsync_WhenPlayerIsMissing_ThrowsNotFound()
    {
        var playerId = Guid.NewGuid();
        SetupCacheFactory();
        _repository
            .Setup(repository => repository.GetByIdAndSeasonAsync(
                playerId,
                2026,
                PlayerResponseSize.Extended,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IPlayerResponse?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetNbaPlayerByIdAndYearAsync(
                playerId,
                2026,
                PlayerResponseSize.Extended));

        Assert.Equal($"NBA player '{playerId}' was not found.", exception.Message);
    }

    // Case: Get Nba Player By Id And Year when Id Is Empty
    // Reasoning: This test verifies Get Nba Player By Id And Year under the Id Is Empty condition.
    // Expected Result: The expected outcome is: Does Not Use Cache.
    [Fact]
    public async Task GetNbaPlayerByIdAndYearAsync_WhenIdIsEmpty_DoesNotUseCache()
    {
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.GetNbaPlayerByIdAndYearAsync(
                Guid.Empty,
                2026,
                PlayerResponseSize.Basic));

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

    // Case: Get Nba Players By Name And Year
    // Reasoning: This test verifies the Get Nba Players By Name And Year operation.
    // Expected Result: The expected outcome is: Normalizes And Returns Page.
    [Fact]
    public async Task GetNbaPlayersByNameAndYearAsync_NormalizesAndReturnsPage()
    {
        var player = CreateBasicResponse();
        var pagination = new PaginationRequest { PageSize = 5 };
        var request = new GetNbaPlayersRequest(
            Name: "  LeBron  ",
            Surname: "  JAMES  ",
            Season: 2026);
        _repository
            .Setup(repository => repository.GetPagedNbaPlayersByNameAsync(
                pagination,
                It.Is<GetNbaPlayersRequest>(normalized =>
                    normalized.Name == "lebron" &&
                    normalized.Surname == "james"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(([player], 6));

        var result = await _service.GetNbaPlayersByNameAndYearAsync(
            pagination,
            request);

        Assert.Same(player, Assert.Single(result.Items));
        Assert.Equal(6, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    // Case: Get Nba Players By Name And Year when Without Filter
    // Reasoning: This test verifies Get Nba Players By Name And Year under the Without Filter condition.
    // Expected Result: The expected outcome is: Does Not Query Repository.
    [Fact]
    public async Task GetNbaPlayersByNameAndYearAsync_WithoutFilter_DoesNotQueryRepository()
    {
        var request = new GetNbaPlayersRequest(Season: 2026);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.GetNbaPlayersByNameAndYearAsync(
                new PaginationRequest(),
                request));

        _repository.Verify(repository =>
            repository.GetPagedNbaPlayersByNameAsync(
                It.IsAny<PaginationRequest>(),
                It.IsAny<GetNbaPlayersRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void SetupCacheFactory()
    {
        _cache
            .Setup(cache => cache.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<IPlayerResponse>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                string _,
                Func<CancellationToken, Task<IPlayerResponse>> factory,
                TimeSpan _,
                CancellationToken cancellationToken) => factory(cancellationToken));
    }

    private static NbaPlayerBasicResponse CreateBasicResponse() => new(
        Guid.NewGuid(),
        "LeBron",
        "James",
        "LAL",
        "F");
}
