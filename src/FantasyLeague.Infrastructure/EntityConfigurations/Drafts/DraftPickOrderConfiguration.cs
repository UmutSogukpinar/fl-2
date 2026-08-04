using FantasyLeague.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FantasyLeague.Infrastructure.EntityConfigurations.Drafts;

public sealed class DraftPickOrderConfiguration
    : IEntityTypeConfiguration<DraftPickOrder>
{
    public void Configure(EntityTypeBuilder<DraftPickOrder> builder)
    {
        builder.ToTable("draft_pick_orders");
        builder.HasKey(pick => pick.Id);
        builder.HasIndex(
            pick => new { pick.LeagueId, pick.OverallPick }).IsUnique();
        builder.HasIndex(
            pick => new {
                pick.LeagueId, pick.Round, pick.PositionInRound }).IsUnique();
        builder.HasIndex(
            pick => new { pick.LeagueId, pick.NbaPlayerId })
            .IsUnique()
            .HasFilter("\"NbaPlayerId\" IS NOT NULL");
        builder.Property(pick => pick.NbaPlayerId).IsConcurrencyToken();

        builder.HasOne<League>()
            .WithMany()
            .HasForeignKey(pick => pick.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<FantasyTeam>()
            .WithMany()
            .HasForeignKey(pick => new { pick.TeamId, pick.LeagueId })
            .HasPrincipalKey(team => new { team.Id, team.LeagueId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<NbaPlayer>()
            .WithMany()
            .HasForeignKey(pick => pick.NbaPlayerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
