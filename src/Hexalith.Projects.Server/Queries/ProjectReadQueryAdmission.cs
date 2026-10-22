namespace Hexalith.Projects.Server.Queries;

using System.Security.Claims;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Ui;

/// <summary>Shares authenticated envelope, role and fresh Tenant admission across supported reads.</summary>
internal static class ProjectReadQueryAdmission
{
    /// <summary>Admits the exact query using the deployment audience and required read scope.</summary>
    internal static async Task<ProjectAuthorizationResult?> AuthorizeAsync(QueryEnvelope query, string queryType, bool list,
        ProjectAuthorizationGate gate, ProjectQueryEnvelopePrincipalBinding binding, CancellationToken cancellationToken)
    {
        if (query.Domain != ProjectsServerModule.DomainName || query.QueryType != queryType
            || string.IsNullOrWhiteSpace(query.OriginalActorId) || string.IsNullOrWhiteSpace(query.AuthenticatedWorkloadId)
            || query.IsDelegated && string.IsNullOrWhiteSpace(query.DelegationId)
            || !query.IsDelegated && query.DelegationId is not null
            || query.Scopes?.Contains(list ? "projects.list" : "projects.read", StringComparer.Ordinal) != true
            || string.IsNullOrWhiteSpace(binding.ExpectedAudience)
            || query.Audience?.Contains(binding.ExpectedAudience, StringComparer.Ordinal) != true
            || !binding.TryBind(query, out IProjectTenantContextAccessor tenantContext) || binding.HttpContext is not { } http)
            return null;
        bool chatbot = IsChatbot(query, binding);
        if (query.AuthenticatedWorkloadId == "chatbot" && !chatbot) return null;
        if (!chatbot && !HasRole(http.User, "TenantOperator") && !HasRole(http.User, "TenantProjectAdministrator")) return null;
        ProjectAuthorizationResult result = await gate.AuthorizeSupportedScopeAsync(list, tenantContext, http,
            query.CorrelationId, cancellationToken).ConfigureAwait(false);
        if (!result.IsAllowed) return result;
        return result.TenantAccessResult is { IsAllowed: true, FreshnessStatus: TenantProjectionFreshnessStatus.Fresh } tenant
            && tenant.TenantId == query.TenantId && !string.IsNullOrWhiteSpace(tenant.ProjectionWatermark)
                ? result : result with { IsAllowed = false, Retryable = true, Reason = ReferenceState.Unavailable };
    }

    /// <summary>Recognizes an authenticated delegated Chatbot with its required ProjectUser role.</summary>
    internal static bool IsChatbot(QueryEnvelope query, ProjectQueryEnvelopePrincipalBinding binding)
        => query.AuthenticatedWorkloadId == "chatbot" && query.IsDelegated && !string.IsNullOrWhiteSpace(query.DelegationId)
            && binding.HttpContext is { } http && HasRole(http.User, "ProjectUser");

    private static bool HasRole(ClaimsPrincipal principal, string role)
        => principal.IsInRole(role) || principal.HasClaim("role", role) || principal.HasClaim("projects:role", role);
}
