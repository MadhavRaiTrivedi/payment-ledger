namespace PaymentLedger.Domain;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
