namespace Hexalith.Projects.Server;

using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Server.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Retains required v1 names only after explicit, separately authorized durable inspection.</summary>
public static partial class ProjectsDomainServiceEndpoints
{
    private static async Task<bool> AuthorizeLegacyInspectionAsync(HttpContext context,
        IProjectTenantContextAccessor tenant, string correlationId, string queryType, string target,
        int count, string observedRevision, CancellationToken cancellationToken)
    {
        if (context.Request.Query["inspectDescriptiveMetadata"] is not { Count: 1 } optIn || optIn[0] != "true"
            || !context.User.HasClaim("eventstore:permission", ProjectReadQueryExecutor.InspectionPermission)
            || string.IsNullOrWhiteSpace(tenant.PrincipalId) || string.IsNullOrWhiteSpace(tenant.AuthoritativeTenantId)) return false;
        var identity = DualPrincipalClaimsHelper.Extract(context.User, tenant.PrincipalId);
        var query = new QueryEnvelope(tenant.AuthoritativeTenantId, "projects", target, queryType,
            "{\"inspectDescriptiveMetadata\":true}"u8.ToArray(), correlationId, tenant.PrincipalId)
        {
            OriginalActorId = tenant.PrincipalId, AuthenticatedWorkloadId = identity.AuthenticatedWorkloadId,
            IsDelegated = identity.IsDelegated, DelegationId = identity.DelegationId,
            Scopes = identity.Scopes, Audience = identity.Audience,
        };
        ProjectReadAudit? audit = context.RequestServices.GetService<ProjectReadAudit>();
        return audit is not null && await audit.RecordAsync(query, true, count,
            observedRevision, context.Request.Query["inspectQuarantined"] == "true" || context.Request.Query["includeQuarantined"] == "true", cancellationToken).ConfigureAwait(false);
    }
}
