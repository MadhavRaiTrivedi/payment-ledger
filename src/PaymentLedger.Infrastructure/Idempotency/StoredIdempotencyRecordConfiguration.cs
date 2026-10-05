using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentLedger.Application.Idempotency;

namespace PaymentLedger.Infrastructure.Idempotency;

internal sealed class StoredIdempotencyRecordConfiguration : IEntityTypeConfiguration<StoredIdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<StoredIdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");
        builder.HasKey(record => new { record.OwnerId, record.IdempotencyKey });
        builder.Property(record => record.IdempotencyKey).HasMaxLength(IdempotentCommandRunner.MaxKeyLength);
        builder.Property(record => record.RequestHash).HasMaxLength(64);
        builder.Property(record => record.ResponseBody).HasColumnType("jsonb");
        builder.HasIndex(record => record.ExpiresAt);
    }
}
