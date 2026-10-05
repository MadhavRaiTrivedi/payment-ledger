using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentLedger.Application.Reconciliation;

namespace PaymentLedger.Infrastructure.Persistence.Configurations;

internal sealed class ReconciliationRunConfiguration : IEntityTypeConfiguration<ReconciliationRun>
{
    public void Configure(EntityTypeBuilder<ReconciliationRun> builder)
    {
        builder.HasKey(run => run.Id);
        builder.Property(run => run.Id).ValueGeneratedNever();
        builder.Property(run => run.Outcome).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(run => run.StartedAt);

        builder.Property(run => run.WalletMismatches)
            .HasColumnType("jsonb")
            .HasConversion(
                mismatches => JsonSerializer.Serialize(mismatches, JsonSerializerOptions.Web),
                json => JsonSerializer.Deserialize<List<WalletMismatch>>(json, JsonSerializerOptions.Web) ?? new List<WalletMismatch>(),
                new ValueComparer<List<WalletMismatch>>(
                    (left, right) => left!.SequenceEqual(right!),
                    mismatches => mismatches.Aggregate(0, (hash, mismatch) => HashCode.Combine(hash, mismatch)),
                    mismatches => mismatches.ToList()));
    }
}
