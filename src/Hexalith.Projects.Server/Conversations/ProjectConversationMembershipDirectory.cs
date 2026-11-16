namespace Hexalith.Projects.Server.Conversations;

using System.Text.Json;
using Hexalith.Conversations.Client;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.Serialization;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Microsoft.AspNetCore.Http;

/// <summary>Reuses the native detail read; advisory and hydration pointers never establish membership.</summary>
public sealed class ProjectConversationMembershipDirectory(IConversationClient? direct,
    Func<DelegatedOwnerQueryClient> delegated, IHttpContextAccessor accessor) : IProjectConversationMembershipDirectory
{
    /// <inheritdoc/>
    public async Task<bool> IsCurrentMemberAsync(string tenantId, string projectId, string conversationId,
        string originalActorId, string correlationId, CancellationToken cancellationToken)
    {
        try
        {
            ConversationDetailResult? detail;
            if (VerifiedQueryCaller.Get(accessor.HttpContext) is { } caller)
            {
                if (caller.Query.TenantId != tenantId || caller.Query.UserId != originalActorId
                    || caller.Query.OriginalActorId != originalActorId) return false;
                QueryResult result = await delegated().InvokeAsync(new SubmitQueryRequest(tenantId, "conversations",
                    conversationId, GetConversationQuery.QueryType,
                    Payload: JsonSerializer.SerializeToElement(new { conversationId, nativeMembershipOnly = true })), cancellationToken).ConfigureAwait(false);
                if (!result.Success) return false;
                detail = JsonSerializer.Deserialize<ConversationDetailResult>(result.PayloadBytes!, JsonSerializerOptions.Web);
            }
            else
            {
                if (direct is null) return false;
                ConversationClientResult<ConversationDetailResult> result = await direct.GetConversationAsync(
                    new GetConversationQuery(SchemaVersion.Current, new TenantId(tenantId), originalActorId,
                        correlationId, new ConversationId(conversationId)) { NativeMembershipOnly = true }, cancellationToken).ConfigureAwait(false);
                if (!result.IsSuccess) return false;
                detail = result.Value;
            }
            return detail is { FreshnessState: var state, Details: { } value }
                && detail.ReadPurpose == GetConversationQuery.NativeMembershipPurpose
                && state == ProjectionTrustState.Current && value.Freshness.FreshnessState == ProjectionTrustState.Current
                && value.TenantId.Value == tenantId && value.ConversationId.Value == conversationId
                && value.ProjectId?.Value == projectId;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception) { return false; }
    }
}
