using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentLedger.Domain.Transactions;

namespace PaymentLedger.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    private const int CorrelationIdMaxLength = 128;
    private const int IdempotencyKeyMaxLength = 128;

    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(transaction => transaction.Id);
        builder.Property(transaction => transaction.Id).ValueGeneratedNever();
        builder.Property(transaction => transaction.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(transaction => transaction.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(transaction => transaction.FailureReason).HasConversion<string>().HasMaxLength(32);
        builder.Property(transaction => transaction.Currency).HasConversion<string>().HasMaxLength(3);
        builder.Property(transaction => transaction.CorrelationId).HasMaxLength(CorrelationIdMaxLength);
        builder.Property(transaction => transaction.IdempotencyKey).HasMaxLength(IdempotencyKeyMaxLength);
        builder.HasIndex(transaction => transaction.OriginalTransactionId);
        builder.Ignore(transaction => transaction.DomainEvents);

        builder.HasMany(transaction => transaction.Entries)
            .WithOne()
            .HasForeignKey(entry => entry.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(transaction => transaction.Entries).HasField("_entries");

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_transactions_refund_within_amount", "refunded_in_paise BETWEEN 0 AND amount_in_paise"));
    }
}
