using Microsoft.EntityFrameworkCore;
using PaymentLedger.Infrastructure.Persistence;

namespace PaymentLedger.Infrastructure.Messaging;

public sealed class ProcessedMessageStore(LedgerDbContext db, TimeProvider clock)
{
    // RabbitMQ delivers at least once, so a consumer must recognise a message it has already handled.
    public async Task<bool> TryMarkProcessedAsync(string consumer, Guid messageId, CancellationToken cancellationToken)
    {
        var insertedRows = await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO processed_messages (consumer, message_id, processed_at)
            VALUES ({consumer}, {messageId}, {clock.GetUtcNow()})
            ON CONFLICT DO NOTHING
            """,
            cancellationToken);

        return insertedRows == 1;
    }
}
