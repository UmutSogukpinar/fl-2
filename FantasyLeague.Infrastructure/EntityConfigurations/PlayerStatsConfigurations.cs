using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Infrastructure.EntityConfigurations;

public sealed class PlayerStatsConfiguration
    : IEntityTypeConfiguration<PlayerStats>
{
    public void Configure(EntityTypeBuilder<PlayerStats> builder)
    {
        builder.ToTable("player_stats");

        builder.HasKey(playerStats => new
        {
            playerStats.NbaPlayerId,
            playerStats.Season
        });

        builder.HasOne<NbaPlayer>()
            .WithMany(player => player.SeasonStats)
            .HasForeignKey(playerStats => playerStats.NbaPlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(playerStats => playerStats.NbaPlayerId)
            .IsRequired();

        builder.Property(playerStats => playerStats.Season)
            .IsRequired();

        builder.Property(playerStats => playerStats.GamesPlayed)
            .IsRequired();

        builder.Property(playerStats => playerStats.GamesStarted)
            .IsRequired();

        builder.Property(playerStats => playerStats.MinutesPerGame)
            .IsRequired();

        builder.Property(playerStats => playerStats.PointsPerGame)
            .IsRequired();

        builder.Property(playerStats => playerStats.ReboundsPerGame)
            .IsRequired();

        builder.Property(playerStats => playerStats.AssistsPerGame)
            .IsRequired();

        builder.Property(playerStats => playerStats.StealsPerGame)
            .IsRequired();

        builder.Property(playerStats => playerStats.BlocksPerGame)
            .IsRequired();

        builder.Property(playerStats => playerStats.TurnoversPerGame)
            .IsRequired();

        builder.Property(playerStats => playerStats.FieldGoalPercentage)
            .IsRequired();

        builder.Property(playerStats => playerStats.ThreePointPercentage)
            .IsRequired();

        builder.Property(playerStats => playerStats.FreeThrowPercentage)
            .IsRequired();
    }
}
