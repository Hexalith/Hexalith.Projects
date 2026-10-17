namespace Hexalith.Projects.Server.Folders;

using System.Text.Json;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.Folders.Contracts;
using Hexalith.Projects.Contracts.Identifiers;
using Microsoft.AspNetCore.Http;

/// <summary>Uses verified delegated owner authorization on workload queries, preserving the existing direct-caller adapter.</summary>
public sealed class DelegatedProjectFolderDirectory(Func<DelegatedOwnerQueryClient> client, IHttpContextAccessor accessor,
    IProjectFolderDirectory directCaller) : IProjectFolderDirectory
{
    public Task<ProjectFolderValidationResult> ValidateSetProjectFolderAsync(ProjectId projectId, string folderId, string correlationId, CancellationToken cancellationToken = default)
        => directCaller.ValidateSetProjectFolderAsync(projectId, folderId, correlationId, cancellationToken);

    public async Task<ProjectFolderValidationResult> RefreshFolderReferenceAsync(ProjectId projectId, string folderId, string correlationId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projectId);
        VerifiedQueryCaller? caller = VerifiedQueryCaller.Get(accessor.HttpContext);
        if (caller is null)
            return await directCaller.RefreshFolderReferenceAsync(projectId, folderId, correlationId, cancellationToken).ConfigureAwait(false);
        try
        {
            var query = new SubmitQueryRequest(caller.Query.TenantId, "folders", folderId, FolderOwnerReadQuery.QueryType,
                Payload: JsonSerializer.SerializeToElement(new FolderOwnerReadQuery()));
            QueryResult result = await client().InvokeAsync(query, cancellationToken).ConfigureAwait(false);
            if (!result.Success) return new(result.ErrorMessage == "safe-denial" ? ProjectFolderValidationOutcome.Denied : ProjectFolderValidationOutcome.Unavailable, correlationId);
            JsonElement payload = result.GetPayload();
            if (payload.ValueKind != JsonValueKind.Object) return new(ProjectFolderValidationOutcome.Unavailable, correlationId);
            JsonProperty[] outcomes = payload.EnumerateObject().Where(static property => property.Name.Equals("outcome", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (outcomes.Length != 1 || outcomes[0].Value.ValueKind != JsonValueKind.Number
                || !outcomes[0].Value.TryGetInt32(out int outcome) || !Enum.IsDefined((FolderOwnerReadOutcome)outcome))
                return new(ProjectFolderValidationOutcome.Unavailable, correlationId);
            return new((FolderOwnerReadOutcome)outcome switch
            {
                FolderOwnerReadOutcome.Allowed => ProjectFolderValidationOutcome.Accepted,
                FolderOwnerReadOutcome.Stale => ProjectFolderValidationOutcome.Stale,
                _ => ProjectFolderValidationOutcome.Unavailable,
            }, correlationId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(ProjectFolderValidationOutcome.Unavailable, correlationId); }
    }
}
