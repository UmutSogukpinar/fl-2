using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Infrastructure.EntityConfigurations;

public sealed class UserConfiguration
    : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Username)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(user => user.Email)
            .IsRequired();

        builder.Property(user => user.TimeZoneId)
            .HasMaxLength(100)
            .HasDefaultValue("UTC")
            .IsRequired();
    }
}
