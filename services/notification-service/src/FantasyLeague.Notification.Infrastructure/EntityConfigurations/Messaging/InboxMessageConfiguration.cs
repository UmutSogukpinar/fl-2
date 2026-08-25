using FantasyLeague.Notification.Infrastructure.Messaging.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FantasyLeague.Notification.Infrastructure.EntityConfigurations.Messaging;

public sealed class InboxMessageConfiguration
    : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");

        builder.HasKey(message => message.MessageId);

        builder.Property(message => message.MessageId)
            .HasMaxLength(128)
            .IsRequired();
        builder.Property(message => message.MessageType)
            .HasMaxLength(500)
            .IsRequired();
        builder.Property(message => message.Payload)
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(message => message.ReceivedOnUtc).IsRequired();
        builder.Property(message => message.ProcessedOnUtc).IsRequired(false);
        builder.Property(message => message.AttemptCount)
            .HasDefaultValue(0)
            .IsRequired();
        builder.Property(message => message.LastError).IsRequired(false);

        builder.HasIndex(message => new
        {
            message.ProcessedOnUtc,
            message.ReceivedOnUtc
        });
    }
}
