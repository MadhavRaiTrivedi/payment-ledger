using Microsoft.EntityFrameworkCore;
using PaymentLedger.Application.Reconciliation;
using PaymentLedger.Domain.Accounts;
using PaymentLedger.Domain.Holds;
using PaymentLedger.Domain.Transactions;
using PaymentLedger.Infrastructure.Idempotency;
using PaymentLedger.Infrastructure.Messaging;
using PaymentLedger.Infrastructure.Outbox;

namespace PaymentLedger.Infrastructure.Persistence;

public sealed class LedgerDbContext(DbContextOptions<LedgerDbContext> options) : DbContext(options)
{
    public const string ConnectionStringName = "Ledger";

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<Entry> Entries => Set<Entry>();

    public DbSet<Hold> Holds => Set<Hold>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<StoredIdempotencyRecord> IdempotencyRecords => Set<StoredIdempotencyRecord>();

    public DbSet<ReconciliationRun> ReconciliationRuns => Set<ReconciliationRun>();

    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LedgerDbContext).Assembly);
}
