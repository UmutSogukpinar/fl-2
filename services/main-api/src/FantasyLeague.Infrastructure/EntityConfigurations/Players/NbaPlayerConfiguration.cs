using FantasyLeague.Domain.Entities.Players;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Infrastructure.EntityConfigurations.Players;

public sealed class NbaPlayerConfiguration
    : IEntityTypeConfiguration<NbaPlayer>
{
    public void Configure(EntityTypeBuilder<NbaPlayer> builder)
    {
        builder.ToTable("nba_players");

        builder.HasKey(player => player.Id);

        builder.HasIndex(player => player.NbaId)
            .IsUnique();

        builder.Property(player => player.FirstName)
            .IsRequired();

        builder.Property(player => player.LastName)
            .IsRequired();

        builder.HasIndex(player => player.FirstName);
        builder.HasIndex(player => player.LastName);


    }
}
