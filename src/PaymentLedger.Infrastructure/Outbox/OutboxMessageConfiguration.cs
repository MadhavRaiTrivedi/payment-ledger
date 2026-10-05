using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PaymentLedger.Infrastructure.Outbox;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();
        builder.Property(message => message.Type).HasMaxLength(128);
        builder.Property(message => message.Payload).HasColumnType("jsonb");

        builder.HasIndex(message => message.OccurredAt)
            .HasFilter("published_at IS NULL")
            .HasDatabaseName("ix_outbox_messages_unpublished");
    }
}
