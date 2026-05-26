// <copyright file="ProjectFolderValidationResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Folders;

/// <summary>Safe outcome categories for Projects folder-reference validation.</summary>
public enum ProjectFolderValidationOutcome
{
    /// <summary>The Folders boundary confirmed the reference is currently usable.</summary>
    Accepted,

    /// <summary>The Projects-shaped request is invalid.</summary>
    ValidationFailed,

    /// <summary>The referenced folder is archived or otherwise inactive.</summary>
    Archived,

    /// <summary>The folder evidence is stale and must not be trusted for a mutation.</summary>
    Stale,

    /// <summary>The caller or folder is denied/hidden by Folders.</summary>
    Denied,

    /// <summary>The Folders boundary is unavailable or returned untrusted evidence.</summary>
    Unavailable,
}

/// <summary>Safe result from the Projects-to-Folders ACL boundary.</summary>
/// <param name="Outcome">The outcome category.</param>
/// <param name="CorrelationId">A safe correlation identifier.</param>
public sealed record ProjectFolderValidationResult(
    ProjectFolderValidationOutcome Outcome,
    string? CorrelationId)
{
    /// <summary>Creates an accepted result.</summary>
    /// <param name="correlationId">The safe correlation identifier.</param>
    /// <returns>An accepted validation result.</returns>
    public static ProjectFolderValidationResult Accepted(string? correlationId)
        => new(ProjectFolderValidationOutcome.Accepted, correlationId);
}
