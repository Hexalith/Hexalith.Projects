namespace Hexalith.Projects.Server;

using System.Collections.Concurrent;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Queries;

/// <summary>Bounds owner and Project authorization on the retained resolution REST adapters.</summary>
public static partial class ProjectsDomainServiceEndpoints
{
    private static async Task<HashSet<string>?> AuthorizeLegacyResolutionCandidatesAsync(IEnumerable<string> projectIds,
        ProjectAuthorizationGate gate, IProjectTenantContextAccessor tenant, HttpContext http,
        string? correlationId, string? taskId, CancellationToken token, IProjectFolderDirectory? folders = null)
    {
        ConcurrentBag<string> allowed = [];
        int unavailable = 0;
        await Parallel.ForEachAsync(projectIds.Distinct(StringComparer.Ordinal), new ParallelOptions
        { MaxDegreeOfParallelism = ProjectAdvisoryQueryExecutor.MaximumConcurrency, CancellationToken = token }, async (id, ct) =>
        {
            var decision = await gate.AuthorizeReadAsync(id, tenant, http, correlationId, taskId, ct).ConfigureAwait(false);
            if (decision.Retryable) Interlocked.Exchange(ref unavailable, 1);
            if (!decision.IsAllowed) return;
            if (folders is not null)
            {
                if (decision.ProjectDetail?.ProjectFolder is not { ReferenceState: ReferenceState.Included, FolderId: { } folderId }
                    || !IsCanonicalIdentifier(folderId)) return;
                var folder = await folders.RefreshFolderReferenceAsync(new ProjectId(id), folderId,
                    correlationId ?? http.TraceIdentifier, ct).ConfigureAwait(false);
                if (folder.Outcome != ProjectFolderValidationOutcome.Accepted)
                {
                    if (folder.Outcome != ProjectFolderValidationOutcome.Denied) Interlocked.Exchange(ref unavailable, 1);
                    return;
                }
            }
            allowed.Add(id);
        }).ConfigureAwait(false);
        return unavailable == 0 ? allowed.ToHashSet(StringComparer.Ordinal) : null;
    }

    private static async Task<string[]?> AuthorizeLegacyResolutionFoldersAsync(IReadOnlyList<string> folderIds,
        IProjectFolderDirectory folders, string? correlationId, CancellationToken token)
    {
        ConcurrentBag<string> allowed = [];
        int unavailable = 0;
        await Parallel.ForEachAsync(folderIds, new ParallelOptions
        { MaxDegreeOfParallelism = ProjectAdvisoryQueryExecutor.MaximumConcurrency, CancellationToken = token }, async (id, ct) =>
        {
            var decision = await folders.RefreshFolderReferenceAsync(new ProjectId("projects"), id, correlationId ?? string.Empty, ct).ConfigureAwait(false);
            if (decision.Outcome == ProjectFolderValidationOutcome.Accepted) allowed.Add(id);
            else if (decision.Outcome != ProjectFolderValidationOutcome.Denied) Interlocked.Exchange(ref unavailable, 1);
        }).ConfigureAwait(false);
        return unavailable == 0 ? allowed.Order(StringComparer.Ordinal).ToArray() : null;
    }
}
