// <copyright file="ProjectMemoryValidationResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Memories;

/// <summary>Safe outcome categories for Projects memory-reference validation against the Memories ACL.</summary>
public enum ProjectMemoryValidationOutcome
{
    /// <summary>The Memories boundary confirmed the memory reference is currently usable (metadata-only).</summary>
    Accepted,

    /// <summary>The Projects-shaped request is invalid.</summary>
    ValidationFailed,

    /// <summary>The caller or case is denied/hidden/missing by Memories.</summary>
    Denied,

    /// <summary>The referenced case is closed, deleting, or otherwise inactive.</summary>
    Archived,

    /// <summary>The case evidence is stale and must not be trusted for a mutation.</summary>
    Stale,

    /// <summary>The case belongs to a different tenant than the owning project.</summary>
    TenantMismatch,

    /// <summary>The Memories boundary is unavailable or returned untrusted evidence.</summary>
    Unavailable,
}

/// <summary>Safe result from the Projects-to-Memories memory-reference ACL boundary.</summary>
/// <param name="Outcome">The outcome category.</param>
/// <param name="CorrelationId">A safe correlation identifier.</param>
public sealed record ProjectMemoryValidationResult(
    ProjectMemoryValidationOutcome Outcome,
    string? CorrelationId)
{
    /// <summary>Creates an accepted result.</summary>
    /// <param name="correlationId">The safe correlation identifier.</param>
    /// <returns>An accepted validation result.</returns>
    public static ProjectMemoryValidationResult Accepted(string? correlationId)
        => new(ProjectMemoryValidationOutcome.Accepted, correlationId);
}
