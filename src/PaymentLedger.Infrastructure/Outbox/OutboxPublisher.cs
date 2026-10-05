using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaymentLedger.Application.Observability;
using PaymentLedger.Infrastructure.Messaging;
using PaymentLedger.Infrastructure.Persistence;

namespace PaymentLedger.Infrastructure.Outbox;

public sealed class OutboxPublisher(
    LedgerDbContext db,
    RabbitMqPublisher publisher,
    LedgerMetrics metrics,
    IOptions<OutboxOptions> options,
    TimeProvider clock,
    ILogger<OutboxPublisher> logger)
{
    public async Task<int> PublishPendingAsync(CancellationToken cancellationToken)
    {
        var batchSize = options.Value.BatchSize;
        var maxAttempts = options.Value.MaxPublishAttempts;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // SKIP LOCKED lets several worker instances drain the outbox without publishing the same row twice.
        var pending = await db.OutboxMessages
            .FromSql(
                $"""
                SELECT * FROM outbox_messages
                WHERE published_at IS NULL AND publish_attempts < {maxAttempts}
                ORDER BY occurred_at
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        var publishedCount = 0;
        foreach (var message in pending)
        {
            try
            {
                await publisher.PublishAsync(message, cancellationToken);
                message.MarkPublished(clock.GetUtcNow());
                publishedCount++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                message.RecordFailure(exception.Message);
                logger.LogWarning(
                    exception,
                    "Publishing outbox message {MessageId} failed on attempt {Attempt}",
                    message.Id,
                    message.PublishAttempts);
                break;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        metrics.MessagesPublished(publishedCount);
        return publishedCount;
    }
}
