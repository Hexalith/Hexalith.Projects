// <copyright file="ProjectTenantIsolationConformance.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Testing.TenantIsolation;

/// <summary>Reusable cross-tenant isolation harness for asserting that tenant A never leaks into tenant B.</summary>
public static class ProjectTenantIsolationConformance
{
    /// <summary>Asserts all supplied isolation surfaces return no cross-tenant leakage.</summary>
    /// <param name="surfaces">The surfaces to evaluate.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous assertion.</returns>
    /// <exception cref="ProjectTenantIsolationConformanceException">Thrown when a surface leaks tenant-owned data.</exception>
    public static async Task AssertNoLeakageAsync(
        IEnumerable<ProjectTenantIsolationSurface> surfaces,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(surfaces);

        foreach (ProjectTenantIsolationSurface surface in surfaces)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProjectTenantIsolationResult result = await surface.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            if (result.Leaked)
            {
                throw new ProjectTenantIsolationConformanceException(surface.Name, result);
            }
        }
    }
}

/// <summary>A named tenant-isolation surface under test.</summary>
/// <param name="Name">The surface name.</param>
/// <param name="ExecuteAsync">Executes the surface and returns whether it leaked.</param>
public sealed record ProjectTenantIsolationSurface(
    string Name,
    Func<CancellationToken, Task<ProjectTenantIsolationResult>> ExecuteAsync);

/// <summary>Result from one tenant-isolation surface.</summary>
/// <param name="Leaked">Whether tenant-owned data crossed into another tenant boundary.</param>
/// <param name="TenantId">The leaked tenant id, when known.</param>
/// <param name="ProjectId">The leaked project id, when known.</param>
/// <param name="Boundary">The surface boundary that was checked.</param>
public sealed record ProjectTenantIsolationResult(
    bool Leaked,
    string? TenantId,
    string? ProjectId,
    string Boundary)
{
    /// <summary>Creates a no-leak result.</summary>
    public static ProjectTenantIsolationResult NoLeak(string boundary)
        => new(false, null, null, boundary);

    /// <summary>Creates a leak result.</summary>
    public static ProjectTenantIsolationResult Leak(string boundary, string? tenantId, string? projectId)
        => new(true, tenantId, projectId, boundary);
}

/// <summary>Thrown when a tenant-isolation surface leaks cross-tenant data.</summary>
public sealed class ProjectTenantIsolationConformanceException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="ProjectTenantIsolationConformanceException"/> class.</summary>
    /// <param name="surfaceName">The failing surface.</param>
    /// <param name="result">The leak result.</param>
    public ProjectTenantIsolationConformanceException(string surfaceName, ProjectTenantIsolationResult result)
        : base(CreateMessage(surfaceName, result))
    {
    }

    private static string CreateMessage(string surfaceName, ProjectTenantIsolationResult result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(surfaceName);
        ArgumentNullException.ThrowIfNull(result);
        return $"ProjectTenantIsolationConformance violation on '{surfaceName}' ({result.Boundary}).";
    }
}
