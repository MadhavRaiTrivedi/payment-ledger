using Microsoft.Extensions.Logging;
using PaymentLedger.Application.Accounts;
using PaymentLedger.Application.Persistence;

namespace PaymentLedger.Application.Holds;

public sealed class ExpireHoldsHandler(
    IHoldRepository holds,
    IAccountRepository accounts,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<ExpireHoldsHandler> logger)
{
    public async Task<int> HandleAsync(int batchSize, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var expired = await holds.LockExpiredAsync(now, batchSize, cancellationToken);
        if (expired.Count == 0)
        {
            return 0;
        }

        var wallets = await accounts.LockAsync(expired.Select(hold => hold.AccountId).ToHashSet(), cancellationToken);
        foreach (var hold in expired)
        {
            hold.Expire(wallets.Get(hold.AccountId), now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Expired {HoldCount} holds", expired.Count);
        return expired.Count;
    }
}
