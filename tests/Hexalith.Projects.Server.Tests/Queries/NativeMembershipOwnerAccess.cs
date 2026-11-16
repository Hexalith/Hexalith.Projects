namespace Hexalith.Projects.Server.Tests.Queries;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.TenantAccess;

/// <summary>Independent original-actor Read authority for owner transport tests.</summary>
internal sealed class NativeMembershipOwnerAccess : IConversationTenantAccessService
{
    public List<string?> ObservedActors { get; } = [];
    public ValueTask<ConversationTenantAccessDecision> CheckAccessAsync(ConversationTenantAccessRequirement requirement,
        TenantId? trustedTenantId, string? callerPrincipalId, TenantId? routeTenantId = null, TenantId? commandTenantId = null,
        TenantId? aggregateTenantId = null, TenantId? projectionTenantId = null, TenantId? idempotencyTenantId = null, CancellationToken cancellationToken = default)
    {
        ObservedActors.Add(callerPrincipalId);
        return ValueTask.FromResult(trustedTenantId?.Value == "tenant-a" && callerPrincipalId == "actor-1"
            ? ConversationTenantAccessDecision.Allowed(requirement, trustedTenantId, callerPrincipalId)
            : ConversationTenantAccessDecision.Denied(requirement, trustedTenantId, callerPrincipalId,
                ConversationTenantAccessDenialReason.UnknownTenant));
    }
}
