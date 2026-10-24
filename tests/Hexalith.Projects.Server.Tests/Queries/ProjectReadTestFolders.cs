namespace Hexalith.Projects.Server.Tests.Queries;

using System.Collections.Concurrent;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Server.Folders;

internal sealed class ProjectReadTestFolders : IProjectFolderDirectory
{
    private readonly ConcurrentDictionary<string, byte> _denied = new(StringComparer.Ordinal);
    public void Deny(string id) => _denied[id] = 0;
    public void Allow(string id) => _denied.TryRemove(id, out _);
    public ConcurrentBag<(string ProjectId, string FolderId)> Arguments { get; } = [];
    /// <summary>Gets the correlation IDs observed by actual owner-read calls.</summary>
    public ConcurrentBag<string> CorrelationIds { get; } = [];
    public bool RequireCorrelation { get; set; }
    private int _reads;
    public int Reads => Volatile.Read(ref _reads);
    public Dictionary<string, ProjectFolderValidationOutcome> Outcomes { get; } = new(StringComparer.Ordinal);
    public Func<string, Task>? OnRead { get; set; }
    public Task<ProjectFolderValidationResult> ValidateSetProjectFolderAsync(ProjectId projectId, string folderId, string correlationId, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("A read attempted a Folder mutation validation.");
    public async Task<ProjectFolderValidationResult> RefreshFolderReferenceAsync(ProjectId projectId, string folderId, string correlationId, CancellationToken cancellationToken = default)
    {
        if (RequireCorrelation) ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        Interlocked.Increment(ref _reads);
        Arguments.Add((projectId.Value, folderId));
        CorrelationIds.Add(correlationId);
        if (OnRead is not null) { await OnRead(folderId).ConfigureAwait(false); }
        return new ProjectFolderValidationResult(Outcomes.TryGetValue(folderId, out ProjectFolderValidationOutcome outcome) ? outcome : _denied.ContainsKey(folderId)
            ? ProjectFolderValidationOutcome.Denied : ProjectFolderValidationOutcome.Accepted, correlationId);
    }
}
