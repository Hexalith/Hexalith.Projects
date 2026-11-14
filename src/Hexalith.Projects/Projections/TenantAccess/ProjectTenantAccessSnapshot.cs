namespace Hexalith.Projects.Projections.TenantAccess;

using System.Collections.Generic;

/// <summary>Complete metadata-only tenant access evidence received through the supported event stream.</summary>
/// <param name="Enabled">Whether the authoritative tenant is active.</param>
/// <param name="Principals">The exact current principal and role evidence.</param>
/// <param name="ConfigurationKeys">The current configuration-key names, without values.</param>
public sealed record ProjectTenantAccessSnapshot(
    bool Enabled,
    IReadOnlyList<ProjectTenantPrincipalEvidence> Principals,
    IReadOnlyList<string> ConfigurationKeys);
