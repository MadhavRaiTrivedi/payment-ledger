using PaymentLedger.Application.Errors;
using PaymentLedger.Domain.Accounts;

namespace PaymentLedger.Application.Security;

public sealed record Requester(Guid UserId, bool IsAdmin)
{
    public static readonly Requester System = new(new Guid("00000000-0000-0000-0000-0000000000ff"), IsAdmin: true);

    public bool CanAccess(Account account) => IsAdmin || account.IsOwnedBy(UserId);

    public void EnsureCanAccess(Account account)
    {
        if (!CanAccess(account))
        {
            throw new AccessDeniedException($"You do not have access to account {account.Id}.");
        }
    }

    public void EnsureAdmin()
    {
        if (!IsAdmin)
        {
            throw new AccessDeniedException("This operation requires the Admin role.");
        }
    }
}
