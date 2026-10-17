namespace Hexalith.Projects.Contracts.Queries;

using System.Text.Json.Serialization;
using Hexalith.Projects.Contracts.Ui;

/// <summary>Metadata required to choose a Project without resolving context.</summary>
/// <param name="ProjectId">The authorized opaque identifier.</param>
/// <param name="Name">Authorized descriptive metadata, omitted for safe-metadata callers.</param>
/// <param name="LifecycleState">The Project lifecycle; Archived always prevents context initialization.</param>
/// <param name="ProjectFolderAvailability">The current safe Folder availability classification.</param>
/// <param name="Snapshot">The shared admission evidence, including the aggregate version.</param>
public sealed record ProjectListRow(
    string ProjectId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Name,
    ProjectLifecycle LifecycleState,
    ReferenceState ProjectFolderAvailability,
    AdmissionSnapshot Snapshot);
