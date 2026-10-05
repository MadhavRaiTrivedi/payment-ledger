namespace PaymentLedger.Application.Reconciliation;

public interface ILedgerChecks
{
    // Both checks must read one consistent snapshot, otherwise a transfer committing between them shows up as a false mismatch.
    Task<LedgerCheckResult> RunAsync(CancellationToken cancellationToken);
}
