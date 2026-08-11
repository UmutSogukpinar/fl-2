using FantasyLeague.Domain.Entities.Leagues;
using FantasyLeague.Domain.Entities.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Infrastructure.EntityConfigurations.Leagues;

public sealed class LeagueConfiguration : IEntityTypeConfiguration<League>
{
    public void Configure(EntityTypeBuilder<League> builder)
    {
        builder.ToTable("leagues");

        builder.HasKey(league => league.Id);

        builder.Property(league => league.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(league => league.Description)
            .HasMaxLength(500);

        builder.Property(league => league.Season)
            .IsRequired();

        builder.Property(league => league.MaxTeams)
            .IsRequired();

        builder.Property(league => league.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(league => league.JoinCode)
            .HasMaxLength(8)
            .IsRequired();

        builder.HasIndex(league => league.JoinCode)
            .IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(league => league.CommissionerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
