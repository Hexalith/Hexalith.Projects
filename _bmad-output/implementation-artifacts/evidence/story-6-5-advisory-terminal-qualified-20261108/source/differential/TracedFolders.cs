namespace Story65.AdvisoryWaveProof;

using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Server.Folders;

internal sealed class TracedFolders(TraceLog trace, ProofCase scenario) : IProjectFolderDirectory
{
    public Task<ProjectFolderValidationResult> ValidateSetProjectFolderAsync(ProjectId projectId, string folderId, string correlationId, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Advisory dependency attempted Folder mutation validation.");
    public Task<ProjectFolderValidationResult> RefreshFolderReferenceAsync(ProjectId projectId, string folderId, string correlationId, CancellationToken cancellationToken = default)
    {
        bool input = projectId.Value == "projects";
        string item = input ? folderId : projectId.Value;
        int ordinal = trace.Next((input ? "input-folder:" : "anchor-folder:") + item);
        int wave = input ? ordinal == 1 ? 2 : 5 : ordinal == 1 ? 4 : 6;
        return trace.CallAsync(wave, input ? "input-folder" : "anchor-folder", item, cancellationToken, () =>
        {
            TraceLog.Require(correlationId == "read-request", "Folder correlation changed.");
            bool deny = scenario.Mutation == "deny-folder" || scenario.Mutation == "revoke-input" && wave == 5
                || scenario.Mutation == "anchor-change" && !input && folderId == "new-denied-folder";
            return Task.FromResult(new ProjectFolderValidationResult(deny ? ProjectFolderValidationOutcome.Denied
                : scenario.Mutation == "folder-unavailable" ? ProjectFolderValidationOutcome.Unavailable
                : ProjectFolderValidationOutcome.Accepted, correlationId));
        }, folderId);
    }
}
