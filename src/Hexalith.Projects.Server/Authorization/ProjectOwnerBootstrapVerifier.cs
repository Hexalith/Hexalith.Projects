using System.Text.Json;

using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Projects.Server.Authorization;

/// <summary>Requires current Tenants owner and Parties human identity evidence for each grant change.</summary>
public sealed class ProjectOwnerBootstrapVerifier(
    ProjectChatBotAuthorityGateway gateway, TimeProvider clock,
    ILogger<ProjectOwnerBootstrapVerifier>? logger = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Checks both owners for the exact authenticated issuer and change operation.</summary>
    public async Task<bool> IsCurrentOwnerAsync(string tenantId, string issuerId,
        string operation, CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        string stage = "tenants";
        try
        {
            var tenantPayload = new TenantOwnerRequest(tenantId, issuerId, operation, "human", "Api");
            var tenantQuery = new SubmitQueryRequest("system", "tenants", tenantId,
                "Hexalith.Tenants.Contracts.Queries.ResolveTenantOwnerAuthority",
                Payload: JsonSerializer.SerializeToElement(tenantPayload, JsonOptions));
            SubmitQueryResponse tenantResponse = await gateway.ReadOwnerAuthorityAsync(tenantQuery,
                issuerId, timeout.Token).ConfigureAwait(false);
            TenantOwnerResult? tenant = tenantResponse.Payload.Deserialize<TenantOwnerResult>(JsonOptions);
            // The owner reader stamps its observation during the remote query. Compare it to the
            // local clock after the response, or every fresh observation appears to be in the future.
            DateTimeOffset now = clock.GetUtcNow();
            if (tenant is null || !tenant.Allowed || tenant.TenantId != tenantId ||
                tenant.PrincipalId != issuerId || tenant.Operation != operation ||
                tenant.ActorClass != "human" || tenant.Origin != "Api" ||
                tenant.SourceVersion <= 0 || string.IsNullOrWhiteSpace(tenant.ObservationId) ||
                tenant.ObservedAt > now || now - tenant.ObservedAt > TimeSpan.FromSeconds(60))
            {
                return Denied(stage, "evidence-invalid");
            }

            stage = "party";
            var partyPayload = new PartyRequest(tenantId, issuerId, issuerId, operation, "human", "Api");
            var partyQuery = new SubmitQueryRequest(tenantId, "party", issuerId,
                "Hexalith.Parties.Contracts.Queries.ResolveChatBotPartyAuthority",
                Payload: JsonSerializer.SerializeToElement(partyPayload, JsonOptions));
            SubmitQueryResponse partyResponse = await gateway.ReadOwnerAuthorityAsync(partyQuery,
                issuerId, timeout.Token).ConfigureAwait(false);
            PartyResult? party = partyResponse.Payload.Deserialize<PartyResult>(JsonOptions);
            now = clock.GetUtcNow();
            bool allowed = party is not null && party.Allowed && party.TenantId == tenantId &&
                party.PrincipalId == issuerId && party.PartyId == issuerId &&
                party.Operation == operation && party.ActorClass == "human" && party.Origin == "Api" &&
                party.SourceVersion > 0 && !string.IsNullOrWhiteSpace(party.ObservationId) &&
                party.ObservedAt <= now && now - party.ObservedAt <= TimeSpan.FromSeconds(60);
            return allowed || Denied(stage, "evidence-invalid");
        }
        catch (HttpRequestException exception)
        {
            return Denied(stage, "http-unavailable", (int?)exception.StatusCode ?? 0);
        }
        catch (Exception exception) when (exception is InvalidOperationException or JsonException or ArgumentException)
        {
            return Denied(stage, exception switch
            {
                JsonException => "json-invalid",
                ArgumentException => "request-invalid",
                _ => "owner-result-unavailable",
            });
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Denied(stage, "owner-timeout");
        }
    }

    private bool Denied(string stage, string reasonCode, int statusCode = 0)
    {
        logger?.LogWarning("Project owner bootstrap denied at {Stage}: {ReasonCode} (HTTP {StatusCode}).",
            stage, reasonCode, statusCode);
        return false;
    }
}
