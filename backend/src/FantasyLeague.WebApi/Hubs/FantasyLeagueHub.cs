using Microsoft.AspNetCore.SignalR;

namespace FantasyLeague.WebApi.Hubs;

public sealed class FantasyLeagueHub : Hub
{
    public static string LeagueGroup(Guid leagueId) => $"league:{leagueId:N}";

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", new
        {
            connectionId = Context.ConnectionId,
            connectedAtUtc = DateTimeOffset.UtcNow
        });

        await base.OnConnectedAsync();
    }

    public DateTimeOffset Ping() => DateTimeOffset.UtcNow;

    public Task JoinLeague(Guid leagueId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, LeagueGroup(leagueId));

    public Task LeaveLeague(Guid leagueId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, LeagueGroup(leagueId));
}
