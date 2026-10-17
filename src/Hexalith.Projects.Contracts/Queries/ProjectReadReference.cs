namespace Hexalith.Projects.Contracts.Queries;

using System.Text.Json.Serialization;
using Hexalith.Projects.Contracts.Ui;

/// <summary>A Projects-owned reference summary containing no sibling payload.</summary>
/// <param name="ReferenceKind">ProjectFolder, FileReference, or MemoryReference.</param>
/// <param name="FolderId">The opaque Folder identity when applicable.</param>
/// <param name="FileReferenceId">The opaque File Reference identity when applicable.</param>
/// <param name="MemoryReferenceId">The opaque Memory Reference identity when applicable.</param>
/// <param name="DisplayName">Authorized Projects-owned naming intent.</param>
/// <param name="ReferenceState">The reference inclusion state.</param>
/// <param name="ReasonCode">A closed safe reason, without upstream detail.</param>
/// <param name="ObservedAt">The event-carried observation instant.</param>
public sealed record ProjectReadReference(
    string ReferenceKind,
    string? FolderId,
    string? FileReferenceId,
    string? MemoryReferenceId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? DisplayName,
    ReferenceState ReferenceState,
    string? ReasonCode,
    DateTimeOffset ObservedAt);
