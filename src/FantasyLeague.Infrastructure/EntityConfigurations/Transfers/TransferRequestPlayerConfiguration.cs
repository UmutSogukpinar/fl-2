using FantasyLeague.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FantasyLeague.Infrastructure.EntityConfigurations.Transfers;

public sealed class TransferRequestPlayerConfiguration
    : IEntityTypeConfiguration<TransferRequestPlayer>
{
    public void Configure(EntityTypeBuilder<TransferRequestPlayer> builder)
    {
        builder.ToTable("transfer_request_players");
        builder.HasKey(player => new
        {
            player.TransferRequestId,
            player.FromTeamId,
            player.NbaPlayerId
        });
    }
}
