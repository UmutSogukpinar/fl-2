using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FantasyLeague.Domain.Entities.Auth;

namespace FantasyLeague.Infrastructure.EntityConfigurations.Auth;

public sealed class RefreshTokenConfiguration
    : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(token => token.Token);

        builder.Property(token => token.Token).IsRequired();
        builder.Property(token => token.JwtId).IsRequired();
        builder.Property(token => token.ExpiryDate).IsRequired();
        builder.Property(token => token.Status).IsRequired();
        builder.Property(token => token.UserId).IsRequired();
    }
}
