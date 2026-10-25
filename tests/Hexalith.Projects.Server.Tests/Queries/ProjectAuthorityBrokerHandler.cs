#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using Hexalith.EventStore.ServiceDefaults.Authentication;
using Microsoft.AspNetCore.WebUtilities;

/// <summary>Passively observes every token POST without retaining credentials or preventing real broker rejection.</summary>
internal sealed class ProjectAuthorityBrokerHandler(ProjectAuthorityRoutes routes, HttpMessageHandler inner) : DelegatingHandler(inner)
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method == HttpMethod.Post)
        {
            string form = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var fields = QueryHelpers.ParseQuery(form);
            fields.TryGetValue("authorization_details", out var encoded);
            WorkloadAuthorizationDetails? details = encoded.Count == 1 ? WorkloadAuthorizationDetails.Decode(encoded[0] ?? string.Empty) : null;
            string state = encoded.Count == 0 ? "missing" : details is null ? "malformed" : "profile";
            bool validScope = details is not null && fields.TryGetValue("scope", out var scope) && scope.Count == 1
                && scope[0] == new WorkloadAssertionIssuerOptions().BuildScope(details.Audience, details.Operation);
            string? audience = details is null ? null : details.Audience is "projects" or "folders" or "eventstore" ? details.Audience : "other-audience";
            string? operation = details is null ? null : details.Operation is EventStoreWorkloadOperations.DomainServiceQuery or EventStoreWorkloadOperations.GatewayOwnerAuthorityQuery
                ? details.Operation : "other-operation";
            string[] keys = details?.Bindings.Keys.Select(key => key is EventStoreWorkloadAuthenticationDefaults.TenantBindingClaimType
                or EventStoreWorkloadAuthenticationDefaults.DomainBindingClaimType or QueryCallerAuthorityBinding.DigestClaim ? key : "other-binding")
                .Order(StringComparer.Ordinal).ToArray() ?? [];
            var attempt = new ProjectAuthorityTokenAttempt(state, fields.Count, encoded.Count, audience, operation, keys, validScope);
            routes.TokenRequests.Enqueue(attempt);
            routes.Attempts.Enqueue(attempt.Summary);
        }
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
#endif
