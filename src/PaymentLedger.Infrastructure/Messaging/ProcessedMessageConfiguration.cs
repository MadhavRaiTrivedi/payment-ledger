using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PaymentLedger.Infrastructure.Messaging;

internal sealed class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.HasKey(message => new { message.Consumer, message.MessageId });
        builder.Property(message => message.Consumer).HasMaxLength(64);
    }
}
