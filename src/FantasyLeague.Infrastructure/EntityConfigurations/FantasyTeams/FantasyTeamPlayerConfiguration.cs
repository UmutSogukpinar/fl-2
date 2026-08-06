using FantasyLeague.Domain.Entities.FantasyTeams;
using FantasyLeague.Domain.Entities.Players;

using FantasyLeague.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FantasyLeague.Infrastructure.EntityConfigurations.FantasyTeams;

public sealed class FantasyTeamPlayerConfiguration : IEntityTypeConfiguration<FantasyTeamPlayer>
{
    public void Configure(EntityTypeBuilder<FantasyTeamPlayer> builder)
    {
        builder.ToTable("fantasy_team_players");
        builder.HasKey(player => new { player.FantasyTeamId, player.NbaPlayerId });

        builder.Property(player => player.AcquiredAt).IsRequired();

        builder.HasIndex(player => new { player.LeagueId, player.NbaPlayerId })
            .IsUnique();

        builder.HasOne<FantasyTeam>()
            .WithMany(team => team.Players)
            .HasForeignKey(player => new { player.FantasyTeamId, player.LeagueId })
            .HasPrincipalKey(team => new { team.Id, team.LeagueId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<NbaPlayer>()
            .WithMany()
            .HasForeignKey(player => player.NbaPlayerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
