using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PaymentLedger.Application.Observability;
using PaymentLedger.Domain;
using PaymentLedger.Domain.Transactions;

namespace PaymentLedger.Infrastructure.Outbox;

// Events are written in the same SaveChanges as the entities that raised them, so an event exists
// if and only if the change that caused it was committed.
internal sealed class DomainEventsToOutboxInterceptor(LedgerMetrics metrics) : SaveChangesInterceptor
{
    public static readonly JsonSerializerOptions PayloadSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            MoveDomainEventsToOutbox(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            MoveDomainEventsToOutbox(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    private void MoveDomainEventsToOutbox(DbContext context)
    {
        var aggregates = context.ChangeTracker.Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), PayloadSerializerOptions);
                context.Add(OutboxMessage.Create(domainEvent.GetType().Name, payload, domainEvent.OccurredAt));
                RecordMetric(domainEvent);
            }

            aggregate.ClearDomainEvents();
        }
    }

    private void RecordMetric(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case TransactionPosted posted:
                metrics.TransactionCompleted(posted.Type, TransactionStatus.Posted);
                break;
            case TransactionFailed failed:
                metrics.TransactionCompleted(failed.Type, TransactionStatus.Failed);
                break;
        }
    }
}
