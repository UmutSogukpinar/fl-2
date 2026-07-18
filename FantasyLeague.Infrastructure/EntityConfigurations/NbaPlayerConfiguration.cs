using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Infrastructure.EntityConfigurations;

public sealed class NbaPlayerConfiguration
    : IEntityTypeConfiguration<NbaPlayer>
{
    public void Configure(EntityTypeBuilder<NbaPlayer> builder)
    {
        builder.ToTable("nba_players");

        builder.HasKey(player => player.Id);

        builder.Property(player => player.FirstName)
            .IsRequired();

        builder.Property(player => player.LastName)
            .IsRequired();

    }
}
