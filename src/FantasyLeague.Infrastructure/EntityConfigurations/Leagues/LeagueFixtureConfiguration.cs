using FantasyLeague.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FantasyLeague.Infrastructure.EntityConfigurations.Leagues;

public sealed class LeagueFixtureConfiguration : IEntityTypeConfiguration<LeagueFixture>
{
    public void Configure(EntityTypeBuilder<LeagueFixture> builder)
    {
        builder.ToTable("league_fixtures");

        builder.HasKey(fixture => fixture.Id);

        builder.Property(fixture => fixture.Id)
            .ValueGeneratedOnAdd();

        builder.HasIndex(fixture => new { fixture.LeagueId, fixture.Week });

        builder.HasIndex(fixture => new { fixture.LeagueId, fixture.HomeTeamId, fixture.AwayTeamId })
            .IsUnique();

        builder.HasOne<League>()
            .WithMany()
            .HasForeignKey(fixture => fixture.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<FantasyTeam>()
            .WithMany()
            .HasForeignKey(fixture => new { fixture.HomeTeamId, fixture.LeagueId })
            .HasPrincipalKey(team => new { team.Id, team.LeagueId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FantasyTeam>()
            .WithMany()
            .HasForeignKey(fixture => new { fixture.AwayTeamId, fixture.LeagueId })
            .HasPrincipalKey(team => new { team.Id, team.LeagueId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(fixture => fixture.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

    }
}
