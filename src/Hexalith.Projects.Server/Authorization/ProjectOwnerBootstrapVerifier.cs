using System.Text.Json;

using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Projects.Server.Authorization;

/// <summary>Requires current Tenants owner and Parties human identity evidence for each grant change.</summary>
public sealed class ProjectOwnerBootstrapVerifier(
    ProjectChatBotAuthorityGateway gateway, TimeProvider clock)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Checks both owners for the exact authenticated issuer and change operation.</summary>
    public async Task<bool> IsCurrentOwnerAsync(string tenantId, string issuerId,
        string operation, CancellationToken cancellationToken)
    {
        try
        {
            DateTimeOffset now = clock.GetUtcNow();
            var tenantPayload = new TenantOwnerRequest(tenantId, issuerId, operation, "human", "Api");
            var tenantQuery = new SubmitQueryRequest("system", "tenants", tenantId,
                "Hexalith.Tenants.Contracts.Queries.ResolveTenantOwnerAuthority",
                Payload: JsonSerializer.SerializeToElement(tenantPayload, JsonOptions));
            SubmitQueryResponse tenantResponse = await gateway.ReadOwnerAuthorityAsync(tenantQuery,
                issuerId, cancellationToken).ConfigureAwait(false);
            TenantOwnerResult? tenant = tenantResponse.Payload.Deserialize<TenantOwnerResult>(JsonOptions);
            if (tenant is null || !tenant.Allowed || tenant.TenantId != tenantId ||
                tenant.PrincipalId != issuerId || tenant.Operation != operation ||
                tenant.ActorClass != "human" || tenant.Origin != "Api" ||
                tenant.SourceVersion <= 0 || string.IsNullOrWhiteSpace(tenant.ObservationId) ||
                tenant.ObservedAt > now || now - tenant.ObservedAt > TimeSpan.FromSeconds(60))
            {
                return false;
            }

            var partyPayload = new PartyRequest(tenantId, issuerId, issuerId, operation, "human", "Api");
            var partyQuery = new SubmitQueryRequest(tenantId, "party", issuerId,
                "Hexalith.Parties.Contracts.Queries.ResolveChatBotPartyAuthority",
                Payload: JsonSerializer.SerializeToElement(partyPayload, JsonOptions));
            SubmitQueryResponse partyResponse = await gateway.ReadOwnerAuthorityAsync(partyQuery,
                issuerId, cancellationToken).ConfigureAwait(false);
            PartyResult? party = partyResponse.Payload.Deserialize<PartyResult>(JsonOptions);
            now = clock.GetUtcNow();
            return party is not null && party.Allowed && party.TenantId == tenantId &&
                party.PrincipalId == issuerId && party.PartyId == issuerId &&
                party.Operation == operation && party.ActorClass == "human" && party.Origin == "Api" &&
                party.SourceVersion > 0 && !string.IsNullOrWhiteSpace(party.ObservationId) &&
                party.ObservedAt <= now && now - party.ObservedAt <= TimeSpan.FromSeconds(60);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or ArgumentException)
        {
            return false;
        }
    }
}
