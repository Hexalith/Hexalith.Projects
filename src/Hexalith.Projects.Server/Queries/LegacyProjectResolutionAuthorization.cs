namespace Hexalith.Projects.Server;

using System.Collections.Concurrent;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Queries;

/// <summary>Bounds owner and Project authorization on the retained resolution REST adapters.</summary>
public static partial class ProjectsDomainServiceEndpoints
{
    private static bool HasCurrentLegacyReadAuthority(ProjectAuthorizationResult authorization)
        => authorization.TenantAccessResult is { IsAllowed: true, FreshnessStatus: Hexalith.Projects.Authorization.TenantProjectionFreshnessStatus.Fresh };

    private static Hexalith.Projects.Contracts.Models.ProjectResolution SafeLegacyResolution(Hexalith.Projects.Contracts.Models.ProjectResolution resolution)
        => new(resolution.Result, resolution.Candidates.Select(candidate =>
            new Hexalith.Projects.Contracts.Models.ResolutionCandidate(candidate.ProjectId, null, candidate.ReasonCodes, candidate.Rank, candidate.Score)).ToArray(),
            [], resolution.ObservedAt);

    private static bool IsLegacyResolutionIncomplete(Hexalith.Projects.Contracts.Models.ProjectResolution resolution)
        => resolution.Excluded.Any(exclusion => exclusion.ReferenceState is not
            (ReferenceState.Unauthorized or ReferenceState.Excluded or ReferenceState.Archived));

    private static async Task<IReadOnlyDictionary<string, ProjectDetailItem>?> AuthorizeLegacyResolutionCandidatesAsync(IEnumerable<string> projectIds,
        ProjectAuthorizationGate gate, IProjectTenantContextAccessor tenant, HttpContext http,
        string? correlationId, string? taskId, CancellationToken token, IProjectFolderDirectory? folders = null,
        Predicate<ProjectDetailItem>? matches = null)
    {
        ConcurrentDictionary<string, ProjectDetailItem> allowed = new(StringComparer.Ordinal);
        int unavailable = 0;
        await Parallel.ForEachAsync(projectIds.Distinct(StringComparer.Ordinal), new ParallelOptions
        { MaxDegreeOfParallelism = ProjectAdvisoryQueryExecutor.MaximumConcurrency, CancellationToken = token }, async (id, ct) =>
        {
            var decision = await gate.AuthorizeReadAsync(id, tenant, http, correlationId, taskId, ct).ConfigureAwait(false);
            if (decision.Retryable) Interlocked.Exchange(ref unavailable, 1);
            if (!decision.IsAllowed) return;
            if (decision.ProjectDetail is not { } detail || detail.ProjectId != id)
            {
                Interlocked.Exchange(ref unavailable, 1);
                return;
            }
            if (ProjectReadQueryExecutor.IsQuarantined(detail) || matches?.Invoke(detail) == false) return;
            if (folders is not null && detail.ProjectFolder is { } binding
                && binding is not { ReferenceState: ReferenceState.Pending, FolderId: null })
            {
                if (binding is not { ReferenceState: ReferenceState.Included, FolderId: { } folderId }
                    || !IsCanonicalIdentifier(folderId)) return;
                var folder = await folders.RefreshFolderReferenceAsync(new ProjectId(id), folderId,
                    correlationId ?? http.TraceIdentifier, ct).ConfigureAwait(false);
                if (folder.Outcome != ProjectFolderValidationOutcome.Accepted)
                {
                    if (folder.Outcome is not (ProjectFolderValidationOutcome.Denied or ProjectFolderValidationOutcome.Archived or ProjectFolderValidationOutcome.ValidationFailed)) Interlocked.Exchange(ref unavailable, 1);
                    return;
                }
            }
            allowed.TryAdd(id, detail);
        }).ConfigureAwait(false);
        return unavailable == 0 ? allowed : null;
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
            else if (decision.Outcome is not (ProjectFolderValidationOutcome.Denied or ProjectFolderValidationOutcome.Archived or ProjectFolderValidationOutcome.ValidationFailed)) Interlocked.Exchange(ref unavailable, 1);
        }).ConfigureAwait(false);
        return unavailable == 0 ? allowed.Order(StringComparer.Ordinal).ToArray() : null;
    }
}
