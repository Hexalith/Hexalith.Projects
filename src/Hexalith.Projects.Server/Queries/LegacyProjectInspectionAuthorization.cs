namespace Hexalith.Projects.Server;

using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Server.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Retains required v1 names only after explicit, separately authorized durable inspection.</summary>
public static partial class ProjectsDomainServiceEndpoints
{
    private static bool HasLegacyInspectionAuthority(HttpContext context, IProjectTenantContextAccessor tenant)
        => context.Request.Query["inspectDescriptiveMetadata"] is { Count: 1 } optIn && optIn[0] == "true"
            && context.User.HasClaim("eventstore:permission", ProjectReadQueryExecutor.InspectionPermission)
            && !string.IsNullOrWhiteSpace(tenant.PrincipalId) && !string.IsNullOrWhiteSpace(tenant.AuthoritativeTenantId);

    private static async Task<bool> AuthorizeLegacyInspectionAsync(HttpContext context,
        IProjectTenantContextAccessor tenant, string correlationId, string queryType, string target,
        int count, string observedRevision, CancellationToken cancellationToken, IReadOnlyCollection<string>? projectIds = null)
    {
        if (!HasLegacyInspectionAuthority(context, tenant)) return false;
        string principalId = tenant.PrincipalId!;
        var identity = DualPrincipalClaimsHelper.Extract(context.User, principalId);
        var query = new QueryEnvelope(tenant.AuthoritativeTenantId!, "projects", target, queryType,
            "{\"inspectDescriptiveMetadata\":true}"u8.ToArray(), correlationId, principalId)
        {
            OriginalActorId = principalId, AuthenticatedWorkloadId = identity.AuthenticatedWorkloadId,
            IsDelegated = identity.IsDelegated, DelegationId = identity.DelegationId,
            Scopes = identity.Scopes, Audience = identity.Audience,
        };
        ProjectReadAudit? audit = context.RequestServices.GetService<ProjectReadAudit>();
        if (audit is null) return false;
        if (context.Request.Query["includeQuarantined"] == "true" && projectIds is not null)
        {
            foreach (string project in projectIds)
            {
                string observation = System.Text.Json.JsonSerializer.Serialize(new[] { observedRevision, project });
                if (!await audit.RecordAsync(query, true, 1, observation, true, cancellationToken, [project]).ConfigureAwait(false)) return false;
            }
            return true;
        }
        return await audit.RecordAsync(query, true, count, observedRevision, context.Request.Query["inspectQuarantined"] == "true",
            cancellationToken, projectIds ?? [target]).ConfigureAwait(false);
    }
}
