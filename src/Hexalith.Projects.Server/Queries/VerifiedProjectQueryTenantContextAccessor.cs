namespace Hexalith.Projects.Server.Queries;

using Hexalith.EventStore.DomainService;
using Hexalith.Projects.Authorization;

/// <summary>Consumes only the caller established by exact SDK query authority verification.</summary>
internal sealed class VerifiedProjectQueryTenantContextAccessor(VerifiedQueryCaller caller) : IProjectTenantContextAccessor
{
    public string? AuthoritativeTenantId => caller.Query.TenantId;
    public string? PrincipalId => caller.Query.UserId;
    public EventStoreClaimTransformEvidence GetClaimTransformEvidence(string actionToken)
        => EventStoreClaimTransformEvidence.Allowed(caller.Query.TenantId, caller.Query.UserId, caller.Query.CallerAuthority!.Permissions);
}
