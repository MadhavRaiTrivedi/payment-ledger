using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentLedger.Domain.Accounts;
using PaymentLedger.Domain.Holds;

namespace PaymentLedger.Infrastructure.Persistence.Configurations;

internal sealed class HoldConfiguration : IEntityTypeConfiguration<Hold>
{
    public void Configure(EntityTypeBuilder<Hold> builder)
    {
        builder.HasKey(hold => hold.Id);
        builder.Property(hold => hold.Id).ValueGeneratedNever();
        builder.Property(hold => hold.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(hold => hold.Currency).HasConversion<string>().HasMaxLength(3);

        builder.HasOne<Account>().WithMany().HasForeignKey(hold => hold.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Account>().WithMany().HasForeignKey(hold => hold.BeneficiaryAccountId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(hold => hold.ExpiresAt)
            .HasFilter($"status = '{HoldStatus.Active}'")
            .HasDatabaseName("ix_holds_active_expires_at");
    }
}
