using FantasyLeague.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FantasyLeague.Infrastructure.EntityConfigurations.Transfers;

public sealed class TransferRequestConfiguration : IEntityTypeConfiguration<TransferRequest>
{
    public void Configure(EntityTypeBuilder<TransferRequest> builder)
    {
        builder.ToTable("transfer_requests");
        builder.HasKey(request => request.Id);
        builder.Property(request => request.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasMany(request => request.Players).WithOne()
            .HasForeignKey(player => player.TransferRequestId).OnDelete(DeleteBehavior.Cascade);
    }
}
