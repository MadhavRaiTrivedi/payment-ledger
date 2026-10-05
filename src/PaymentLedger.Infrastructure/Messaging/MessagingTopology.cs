using PaymentLedger.Domain.Transactions;

namespace PaymentLedger.Infrastructure.Messaging;

public static class MessagingTopology
{
    public const string LedgerEventsExchange = "ledger.events";
    public const string DeadLetterExchange = "ledger.dead-letter";
    public const string NotificationsQueue = "ledger.notifications";
    public const string NotificationsDeadLetterQueue = "ledger.notifications.dead-letter";
    public const string AllTransactionEvents = "transaction.*";

    private static readonly Dictionary<string, string> RoutingKeysByMessageType = new()
    {
        [nameof(TransactionPosted)] = "transaction.posted",
        [nameof(TransactionFailed)] = "transaction.failed",
        [nameof(TransactionReversed)] = "transaction.reversed",
    };

    public static string RoutingKeyFor(string messageType) =>
        RoutingKeysByMessageType.TryGetValue(messageType, out var routingKey)
            ? routingKey
            : throw new InvalidOperationException($"No routing key is defined for message type {messageType}.");
}
