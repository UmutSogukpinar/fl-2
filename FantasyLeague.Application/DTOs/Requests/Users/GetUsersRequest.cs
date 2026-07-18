using System.ComponentModel.DataAnnotations;

namespace FantasyLeague.Application.DTOs.Requests.Users;

public sealed class GetUsersRequest
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;
}
