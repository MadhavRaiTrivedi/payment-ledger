using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentLedger.Domain.Accounts;
using PaymentLedger.Domain.Transactions;

namespace PaymentLedger.Infrastructure.Persistence.Configurations;

internal sealed class EntryConfiguration : IEntityTypeConfiguration<Entry>
{
    public void Configure(EntityTypeBuilder<Entry> builder)
    {
        builder.HasKey(entry => entry.Sequence);
        builder.Property(entry => entry.Sequence).UseIdentityAlwaysColumn();
        builder.Property(entry => entry.Direction).HasConversion<string>().HasMaxLength(8);
        builder.Property(entry => entry.Currency).HasConversion<string>().HasMaxLength(3);

        builder.HasOne<Account>().WithMany().HasForeignKey(entry => entry.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entry => new { entry.AccountId, entry.Sequence });

        builder.ToTable(table => table.HasCheckConstraint("ck_entries_amount_positive", "amount_in_paise > 0"));
    }
}
