using System.Diagnostics.Metrics;
using PaymentLedger.Application.Reconciliation;
using PaymentLedger.Domain.Transactions;

namespace PaymentLedger.Application.Observability;

public sealed class LedgerMetrics
{
    public const string MeterName = "PaymentLedger";

    private const string TransactionTypeTag = "transaction.type";
    private const string TransactionStatusTag = "transaction.status";
    private const string OutcomeTag = "reconciliation.outcome";

    private readonly Counter<long> _transactionsCompleted;
    private readonly Counter<long> _reconciliationRuns;
    private readonly Counter<long> _messagesPublished;

    public LedgerMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _transactionsCompleted = meter.CreateCounter<long>(
            "ledger.transactions.completed", description: "Transactions that reached Posted or Failed.");
        _reconciliationRuns = meter.CreateCounter<long>(
            "ledger.reconciliation.runs", description: "Reconciliation runs by outcome.");
        _messagesPublished = meter.CreateCounter<long>(
            "ledger.outbox.published", description: "Outbox messages published to the broker.");
    }

    public void TransactionCompleted(TransactionType type, TransactionStatus status) =>
        _transactionsCompleted.Add(
            1,
            new KeyValuePair<string, object?>(TransactionTypeTag, type.ToString()),
            new KeyValuePair<string, object?>(TransactionStatusTag, status.ToString()));

    public void ReconciliationCompleted(ReconciliationRun run) =>
        _reconciliationRuns.Add(1, new KeyValuePair<string, object?>(OutcomeTag, run.Outcome.ToString()));

    public void MessagesPublished(int count) => _messagesPublished.Add(count);
}
