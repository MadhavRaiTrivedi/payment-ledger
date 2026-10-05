namespace PaymentLedger.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string ConnectionString { get; init; } = "amqp://guest:guest@localhost:5672";

    public string ClientName { get; init; } = "payment-ledger";
}
