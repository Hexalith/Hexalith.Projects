namespace Hexalith.Projects.Contracts.Queries;

using System.Text.Json.Serialization;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;

/// <summary>Authorized Project metadata, validated setup summary, and Projects-owned references.</summary>
/// <param name="ProjectId">The opaque Project identifier.</param>
/// <param name="Name">Authorized descriptive name.</param>
/// <param name="Description">Authorized validated description.</param>
/// <param name="LifecycleState">The lifecycle; Archived prevents context initialization.</param>
/// <param name="CreatedAt">The event-carried creation instant.</param>
/// <param name="UpdatedAt">The event-carried last change instant.</param>
/// <param name="Setup">Authorized validated typed setup.</param>
/// <param name="SetupMetadata">Authorized opaque setup reference.</param>
/// <param name="References">The ordered metadata-only reference summary.</param>
/// <param name="Snapshot">The shared AD-32 admission evidence.</param>
public sealed record GetProjectResponse(
    string ProjectId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Name,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Description,
    ProjectLifecycle LifecycleState,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ProjectSetup? Setup,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? SetupMetadata,
    IReadOnlyList<ProjectReadReference> References,
    AdmissionSnapshot Snapshot);
