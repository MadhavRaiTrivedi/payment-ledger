using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PaymentLedger.Application.Errors;
using PaymentLedger.Application.Persistence;
using PaymentLedger.Application.Security;

namespace PaymentLedger.Application.Idempotency;

public sealed class IdempotentCommandRunner(
    IUnitOfWork unitOfWork,
    IIdempotencyStore store,
    IRequestContext requestContext,
    IOptions<IdempotencyOptions> options,
    TimeProvider clock)
{
    public const int MaxKeyLength = 128;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<IdempotentResult<TResponse>> RunAsync<TCommand, TResponse>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<TResponse>> handle,
        CancellationToken cancellationToken)
    {
        var key = RequireKey();
        var ownerId = requestContext.Requester.UserId;
        var requestHash = HashRequest(command);
        var now = clock.GetUtcNow();

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var claim = new IdempotencyClaim(ownerId, key, requestHash, now, now + options.Value.KeyRetention);
        if (!await store.TryClaimAsync(claim, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return await ReplayAsync<TResponse>(ownerId, key, requestHash, cancellationToken);
        }

        var response = await handle(command, cancellationToken);
        await store.CompleteAsync(ownerId, key, JsonSerializer.Serialize(response, SerializerOptions), cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new IdempotentResult<TResponse>(response, IsReplay: false);
    }

    private async Task<IdempotentResult<TResponse>> ReplayAsync<TResponse>(
        Guid ownerId,
        string key,
        string requestHash,
        CancellationToken cancellationToken)
    {
        var existing = await store.FindAsync(ownerId, key, cancellationToken)
            ?? throw new InvalidOperationException($"Idempotency key '{key}' was claimed but its record is missing.");

        if (existing.RequestHash != requestHash)
        {
            throw new IdempotencyKeyReusedException(key);
        }

        var response = JsonSerializer.Deserialize<TResponse>(existing.ResponseBody!, SerializerOptions)!;
        return new IdempotentResult<TResponse>(response, IsReplay: true);
    }

    private string RequireKey()
    {
        var key = requestContext.IdempotencyKey;
        if (string.IsNullOrWhiteSpace(key) || key.Length > MaxKeyLength)
        {
            throw new InvalidRequestException(
                $"An Idempotency-Key header of 1 to {MaxKeyLength} characters is required for this request.");
        }

        return key;
    }

    private static string HashRequest<TCommand>(TCommand command)
    {
        var payload = $"{typeof(TCommand).Name}:{JsonSerializer.Serialize(command, SerializerOptions)}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }
}
