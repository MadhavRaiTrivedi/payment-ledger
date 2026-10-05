using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PaymentLedger.Api.Security;
using PaymentLedger.Application.Accounts;
using PaymentLedger.Application.Holds;
using PaymentLedger.Application.Transactions;

namespace PaymentLedger.IntegrationTests.Infrastructure;

internal sealed class LedgerClient(HttpClient http, Guid userId)
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public Guid UserId { get; } = userId;

    public HttpClient Http { get; } = http;

    public static Task<LedgerClient> AdminAsync(LedgerApiFactory factory) =>
        CreateAsync(factory, Guid.NewGuid(), UserRole.Admin);

    public static Task<LedgerClient> CustomerAsync(LedgerApiFactory factory, Guid? userId = null) =>
        CreateAsync(factory, userId ?? Guid.NewGuid(), UserRole.Customer);

    public async Task<AccountResponse> OpenWalletAsync(Guid ownerId)
    {
        var response = await Http.PostAsJsonAsync("/api/accounts", new { ownerId }, JsonOptions);
        return await ReadAsync<AccountResponse>(response);
    }

    public Task<AccountResponse> GetAccountAsync(Guid accountId) => GetAsync<AccountResponse>($"/api/accounts/{accountId}");

    public Task<TransactionResponse> GetTransactionAsync(Guid transactionId) =>
        GetAsync<TransactionResponse>($"/api/transactions/{transactionId}");

    public async Task<T> GetAsync<T>(string path) =>
        (await Http.GetFromJsonAsync<T>(path, JsonOptions, TestContext.Current.CancellationToken))!;

    public static async Task<ProblemResponse> ReadProblemAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ProblemResponse>(JsonOptions, TestContext.Current.CancellationToken))!;

    public Task<HttpResponseMessage> DepositAsync(Guid accountId, long amountInPaise, string? idempotencyKey = null) =>
        PostWithKeyAsync("/api/deposits", new { accountId, amountInPaise }, idempotencyKey);

    public Task<HttpResponseMessage> WithdrawAsync(Guid accountId, long amountInPaise, string? idempotencyKey = null) =>
        PostWithKeyAsync("/api/withdrawals", new { accountId, amountInPaise }, idempotencyKey);

    public Task<HttpResponseMessage> TransferAsync(
        Guid sourceAccountId,
        Guid destinationAccountId,
        long amountInPaise,
        string? idempotencyKey = null) =>
        PostWithKeyAsync("/api/transfers", new { sourceAccountId, destinationAccountId, amountInPaise }, idempotencyKey);

    public Task<HttpResponseMessage> RefundAsync(Guid transactionId, long amountInPaise, string? idempotencyKey = null) =>
        PostWithKeyAsync($"/api/transactions/{transactionId}/refunds", new { amountInPaise }, idempotencyKey);

    public Task<HttpResponseMessage> ReverseAsync(Guid transactionId, string? idempotencyKey = null) =>
        PostWithKeyAsync($"/api/transactions/{transactionId}/reversal", new { }, idempotencyKey);

    public Task<HttpResponseMessage> PlaceHoldAsync(
        Guid accountId,
        Guid beneficiaryAccountId,
        long amountInPaise,
        DateTimeOffset expiresAt) =>
        PostWithKeyAsync("/api/holds", new { accountId, beneficiaryAccountId, amountInPaise, expiresAt }, null);

    public Task<HttpResponseMessage> CaptureHoldAsync(Guid holdId, long amountInPaise) =>
        PostWithKeyAsync($"/api/holds/{holdId}/capture", new { amountInPaise }, null);

    public Task<HoldResponse> GetHoldAsync(Guid holdId) => GetAsync<HoldResponse>($"/api/holds/{holdId}");

    public async Task<TransactionResponse> FundAsync(Guid accountId, long amountInPaise) =>
        await ReadAsync<TransactionResponse>(await DepositAsync(accountId, amountInPaise));

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"{(int)response.StatusCode} {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }

        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }

    private Task<HttpResponseMessage> PostWithKeyAsync(string path, object body, string? idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body, options: JsonOptions) };
        request.Headers.Add("Idempotency-Key", idempotencyKey ?? Guid.NewGuid().ToString());
        return Http.SendAsync(request);
    }

    private static async Task<LedgerClient> CreateAsync(LedgerApiFactory factory, Guid userId, UserRole role)
    {
        var http = factory.CreateClient();
        var tokenResponse = await http.PostAsJsonAsync("/api/auth/dev-token", new { userId, role }, JsonOptions);
        var token = await ReadAsync<DevTokenResponse>(tokenResponse);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return new LedgerClient(http, userId);
    }
}
