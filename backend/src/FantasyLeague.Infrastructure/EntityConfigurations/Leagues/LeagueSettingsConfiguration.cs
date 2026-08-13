using FantasyLeague.Domain.Entities.Leagues;

using FantasyLeague.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FantasyLeague.Infrastructure.EntityConfigurations.Leagues;

public sealed class LeagueSettingsConfiguration : IEntityTypeConfiguration<LeagueSettings>
{
    public void Configure(EntityTypeBuilder<LeagueSettings> builder)
    {
        builder.ToTable("league_settings");

        builder.HasKey(settings => settings.LeagueId);

        builder.Property(settings => settings.RosterSize)
            .HasDefaultValue(13)
            .IsRequired();

        builder.Property(settings => settings.DraftDate);

        builder.Property(settings => settings.DraftTimeZoneId)
            .HasMaxLength(100)
            .HasDefaultValue("UTC")
            .IsRequired();

        builder.HasOne<League>()
            .WithOne(league => league.Settings)
            .HasForeignKey<LeagueSettings>(settings => settings.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
