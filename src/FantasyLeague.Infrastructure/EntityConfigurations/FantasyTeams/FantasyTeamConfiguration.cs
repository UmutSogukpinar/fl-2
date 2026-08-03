using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Infrastructure.EntityConfigurations.FantasyTeams;

public sealed class FantasyTeamConfiguration : IEntityTypeConfiguration<FantasyTeam>
{
    public void Configure(EntityTypeBuilder<FantasyTeam> builder)
    {
        builder.ToTable("fantasy_teams");

        builder.HasKey(team => team.Id);

        builder.HasAlternateKey(team => new { team.Id, team.LeagueId });

        builder.Property(team => team.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(team => new { team.LeagueId, team.Name })
            .IsUnique();

        builder.HasIndex(team => new { team.LeagueId, team.OwnerId })
            .IsUnique();

        builder.HasOne<League>()
            .WithMany()
            .HasForeignKey(team => team.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(team => team.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
