namespace Hexalith.Projects.Server.Tests.Queries;

using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Server.Folders;

internal sealed class ProjectReadTestFolders : IProjectFolderDirectory
{
    public HashSet<string> Denied { get; } = new(StringComparer.Ordinal);
    public bool RequireCorrelation { get; set; }
    public int Reads { get; private set; }
    public Dictionary<string, ProjectFolderValidationOutcome> Outcomes { get; } = new(StringComparer.Ordinal);
    public Func<string, Task>? OnRead { get; set; }
    public Task<ProjectFolderValidationResult> ValidateSetProjectFolderAsync(ProjectId projectId, string folderId, string correlationId, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("A read attempted a Folder mutation validation.");
    public async Task<ProjectFolderValidationResult> RefreshFolderReferenceAsync(ProjectId projectId, string folderId, string correlationId, CancellationToken cancellationToken = default)
    {
        if (RequireCorrelation) ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        Reads++;
        if (OnRead is not null) { await OnRead(folderId).ConfigureAwait(false); }
        return new ProjectFolderValidationResult(Outcomes.TryGetValue(folderId, out ProjectFolderValidationOutcome outcome) ? outcome : Denied.Contains(folderId)
            ? ProjectFolderValidationOutcome.Denied : ProjectFolderValidationOutcome.Accepted, correlationId);
    }
}
