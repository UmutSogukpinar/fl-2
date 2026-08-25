using FantasyLeague.Infrastructure.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FantasyLeague.Infrastructure.EntityConfigurations.Messaging;

public sealed class OutboxMessageConfiguration
    : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.PublisherName)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(message => message.MessageType)
            .HasMaxLength(500)
            .IsRequired();
        builder.Property(message => message.Payload)
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(message => message.OccurredOnUtc).IsRequired();
        builder.Property(message => message.ProcessedOnUtc).IsRequired(false);
        builder.Property(message => message.AttemptCount)
            .HasDefaultValue(0)
            .IsRequired();
        builder.Property(message => message.LastError).IsRequired(false);

        builder.HasIndex(message => new
        {
            message.ProcessedOnUtc,
            message.OccurredOnUtc
        });
    }
}
