namespace Hexalith.Projects.Server.Queries;

using Hexalith.Projects.Contracts.Queries;

/// <summary>A committed task observation bound to the caller authorized by the workflow owner.</summary>
/// <param name="TenantId">The authoritative Tenant containing the task.</param>
/// <param name="ActorId">The authenticated actor authorized for this poll.</param>
/// <param name="AdmittedActorId">The original actor bound to the admitted task.</param>
/// <param name="PreActivation">Whether the reserved Project is still private.</param>
/// <param name="Result">The status observed after task authorization.</param>
public sealed record ProjectAuthorizedTaskObservation(string TenantId, string ActorId, string AdmittedActorId, bool PreActivation, ProjectTaskStatusResponse Result);
