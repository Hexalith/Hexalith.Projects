using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.ServiceDefaults.Authentication;

namespace Hexalith.Projects.Server.Authorization;

/// <summary>Reads exact project source and bound bootstrap evidence through EventStore workload routes.</summary>
public sealed class ProjectChatBotAuthorityGateway(
    HttpClient http, IWorkloadAssertionIssuer issuer, IConfiguration configuration,
    TimeProvider clock) : IEventStoreGatewayClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc/>
    public async Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Domain != "projects" || string.IsNullOrWhiteSpace(request.Tenant) ||
            string.IsNullOrWhiteSpace(request.AggregateId))
        {
            throw new InvalidOperationException("Project authority source scope is unavailable.");
        }

        using HttpResponseMessage response = await SendAsync("api/v1/streams/read/workload", request,
            request.Tenant, request.Domain, request.AggregateId!, "eventstore:stream-read",
            "X-Hexalith-Bound-Stream-Read-Proof", cancellationToken).ConfigureAwait(false);
        StreamReadPage? page = await response.Content.ReadFromJsonAsync<StreamReadPage>(JsonOptions, cancellationToken).ConfigureAwait(false);
        if (page is null || page.Tenant != request.Tenant || page.Domain != request.Domain ||
            page.AggregateId != request.AggregateId)
        {
            throw new InvalidOperationException("Project authority source page is mismatched.");
        }

        return page;
    }

    /// <summary>Reads current Tenants/Parties bootstrap evidence for one exact principal.</summary>
    public async Task<SubmitQueryResponse> ReadOwnerAuthorityAsync(SubmitQueryRequest query,
        string principalId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.Domain is not ("tenants" or "party") ||
            query.QueryType is not ("Hexalith.Tenants.Contracts.Queries.ResolveTenantOwnerAuthority" or
                "Hexalith.Parties.Contracts.Queries.ResolveChatBotPartyAuthority") ||
            string.IsNullOrWhiteSpace(principalId))
        {
            throw new InvalidOperationException("Project authority bootstrap scope is unavailable.");
        }

        var bound = new ProjectBoundOwnerQueryRequest(query, principalId);
        string digest = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(bound, JsonOptions)));
        using HttpResponseMessage response = await SendAsync("api/v1/queries/owner-authority/workload", bound,
            query.Tenant, query.Domain, digest, "eventstore:owner-authority-query",
            "X-Hexalith-Bound-Owner-Query-Proof", cancellationToken).ConfigureAwait(false);
        SubmitQueryResponse? result = await response.Content.ReadFromJsonAsync<SubmitQueryResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
        if (result is null || !result.Success || result.Payload.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Project authority bootstrap evidence is unavailable.");
        }

        return result;
    }

    private async Task<HttpResponseMessage> SendAsync<T>(string route, T body, string tenant, string domain,
        string aggregateId, string operation, string proofHeader, CancellationToken cancellationToken)
    {
        string? key = configuration["Authentication:BoundStreamRead:PrivateKey"];
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Project authority signing key is unavailable.");
        }

        string proof;
        try
        {
            using RSA rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(key), out _);
            if (rsa.KeySize < 2048) { throw new InvalidOperationException("Project authority key is too small."); }
            DateTimeOffset now = clock.GetUtcNow();
            var payload = new ProjectBoundReadProofPayload(1, "projects", tenant, domain, aggregateId,
                now.ToUnixTimeSeconds(), now.AddSeconds(30).ToUnixTimeSeconds(),
                Convert.ToHexString(RandomNumberGenerator.GetBytes(16)));
            string encoded = Base64Url(JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions));
            proof = $"{encoded}.{Base64Url(rsa.SignData(Encoding.ASCII.GetBytes(encoded), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))}";
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException or CryptographicException)
        {
            throw new InvalidOperationException("Project authority signing key is invalid.", exception);
        }

        string? assertion = await issuer.IssueAsync(new WorkloadAssertionRequest("eventstore", operation),
            cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(assertion))
        {
            throw new InvalidOperationException("Project authority workload assertion is unavailable.");
        }

        using HttpRequestMessage message = new(HttpMethod.Post, route)
        {
            Content = JsonContent.Create(body, options: JsonOptions),
        };
        message.Headers.TryAddWithoutValidation(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, assertion);
        message.Headers.TryAddWithoutValidation(proofHeader, proof);
        message.Headers.TryAddWithoutValidation("dapr-app-id", "eventstore");
        if (Environment.GetEnvironmentVariable("DAPR_API_TOKEN") is { Length: > 0 } apiToken)
        {
            message.Headers.TryAddWithoutValidation("dapr-api-token", apiToken);
        }

        HttpResponseMessage response = await http.SendAsync(message, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <inheritdoc/>
    public Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
        => Task.FromException<SubmitCommandResponse>(new NotSupportedException());

    /// <inheritdoc/>
    public Task<CommandStatusQueryResponse?> GetCommandStatusAsync(string messageId, CancellationToken cancellationToken = default)
        => Task.FromException<CommandStatusQueryResponse?>(new NotSupportedException());

    /// <inheritdoc/>
    public Task<EventStoreQueryResult> SubmitQueryAsync(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
        => Task.FromException<EventStoreQueryResult>(new NotSupportedException());

    /// <inheritdoc/>
    public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
        => Task.FromException<EventStoreQueryResult<T>>(new NotSupportedException());
}
