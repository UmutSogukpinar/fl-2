using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Infrastructure.EntityConfigurations;

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

        builder.HasOne(league => league.Commissioner)
            .WithMany(user => user.CommissionedLeagues)
            .HasForeignKey(league => league.CommissionerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
