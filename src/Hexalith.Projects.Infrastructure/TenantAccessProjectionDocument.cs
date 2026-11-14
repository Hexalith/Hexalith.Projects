namespace Hexalith.Projects.Infrastructure;

using Hexalith.Projects.Projections.TenantAccess;

/// <summary>The existing tenant-access persisted document shape, including its logical concurrency version.</summary>
/// <param name="Projection">The complete detached authorization projection.</param>
/// <param name="Version">The logical document version.</param>
internal sealed record TenantAccessProjectionDocument(ProjectTenantAccessProjection Projection, long Version);
