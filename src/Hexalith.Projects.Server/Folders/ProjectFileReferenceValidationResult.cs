// <copyright file="ProjectFileReferenceValidationResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Folders;

/// <summary>Safe outcome categories for Projects file-reference validation against the Folders ACL.</summary>
public enum ProjectFileReferenceValidationOutcome
{
    /// <summary>The Folders boundary confirmed the file reference is currently usable (metadata-only).</summary>
    Accepted,

    /// <summary>The Projects-shaped request is invalid.</summary>
    ValidationFailed,

    /// <summary>The caller or file is denied/hidden/missing by Folders.</summary>
    Denied,

    /// <summary>The file metadata is redacted, excluded, or binary-disallowed and must fail closed.</summary>
    Redacted,

    /// <summary>The referenced file or its folder is archived or otherwise inactive.</summary>
    Archived,

    /// <summary>The file evidence is stale and must not be trusted for a mutation.</summary>
    Stale,

    /// <summary>The reference belongs to a different tenant than the owning project.</summary>
    TenantMismatch,

    /// <summary>The Folders boundary is unavailable or returned untrusted evidence.</summary>
    Unavailable,
}

/// <summary>Safe result from the Projects-to-Folders file-reference ACL boundary.</summary>
/// <param name="Outcome">The outcome category.</param>
/// <param name="CorrelationId">A safe correlation identifier.</param>
public sealed record ProjectFileReferenceValidationResult(
    ProjectFileReferenceValidationOutcome Outcome,
    string? CorrelationId)
{
    /// <summary>Creates an accepted result.</summary>
    /// <param name="correlationId">The safe correlation identifier.</param>
    /// <returns>An accepted validation result.</returns>
    public static ProjectFileReferenceValidationResult Accepted(string? correlationId)
        => new(ProjectFileReferenceValidationOutcome.Accepted, correlationId);
}
