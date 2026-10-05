using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentLedger.Domain.Accounts;
using PaymentLedger.Domain.Currencies;

namespace PaymentLedger.Infrastructure.Persistence.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    private static readonly DateTimeOffset SystemAccountsOpenedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(account => account.Id);
        builder.Property(account => account.Id).ValueGeneratedNever();
        builder.Property(account => account.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(account => account.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(account => account.Currency).HasConversion<string>().HasMaxLength(3);
        builder.HasIndex(account => account.OwnerId);

        // Third line of defence after the domain check and the row lock.
        builder.ToTable(table => table.HasCheckConstraint(
            "ck_accounts_wallet_not_overdrawn",
            $"type <> '{AccountType.CustomerWallet}' OR (held_in_paise >= 0 AND balance_in_paise >= held_in_paise)"));

        builder.HasData(
            SystemAccount(SystemAccounts.SettlementId, AccountType.Settlement),
            SystemAccount(SystemAccounts.FeeRevenueId, AccountType.FeeRevenue),
            SystemAccount(SystemAccounts.SuspenseId, AccountType.Suspense));
    }

    private static object SystemAccount(Guid id, AccountType type) => new
    {
        Id = id,
        Type = type,
        Status = AccountStatus.Active,
        OwnerId = (Guid?)null,
        Currency = Currency.INR,
        BalanceInPaise = 0L,
        HeldInPaise = 0L,
        OpenedAt = SystemAccountsOpenedAt,
    };
}
