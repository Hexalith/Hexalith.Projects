// <copyright file="ProjectCommandValidationResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Aggregates.Project;

using Hexalith.Projects.Contracts.Models;

/// <summary>
/// Pure result of <see cref="ProjectCommandValidator"/> (FR-19). Mirrors the Folders
/// <c>FolderCommandValidationResult</c>: an accepted result carries the canonicalized name and the
/// canonical idempotency fingerprint; a rejected result carries the control-flow code and the NAME of
/// the offending field only (never its value).
/// </summary>
/// <param name="IsAccepted">Whether the command passed boundary validation.</param>
/// <param name="Code">The control-flow result code.</param>
/// <param name="CanonicalName">The trimmed, canonical project name (null when rejected).</param>
/// <param name="CanonicalDescription">The trimmed, canonical description, or null (null when rejected or absent).</param>
/// <param name="CanonicalSetupMetadata">The trimmed, canonical setup-metadata reference, or null (null when rejected or absent).</param>
/// <param name="CanonicalSetup">The canonical typed setup, or null when rejected or absent.</param>
/// <param name="IdempotencyFingerprint">The canonical idempotency fingerprint (null when rejected).</param>
/// <param name="RejectedField">The NAME of the rejected field (set only on validation rejection), never its value.</param>
public sealed record ProjectCommandValidationResult(
    bool IsAccepted,
    ProjectResultCode Code,
    string? CanonicalName,
    string? CanonicalDescription,
    string? CanonicalSetupMetadata,
    ProjectSetup? CanonicalSetup,
    string? IdempotencyFingerprint,
    string? RejectedField)
{
    /// <summary>Creates an accepted validation result with the canonical fields and fingerprint.</summary>
    /// <param name="canonicalName">The trimmed, canonical project name.</param>
    /// <param name="canonicalDescription">The trimmed, canonical description or null.</param>
    /// <param name="canonicalSetupMetadata">The trimmed, canonical setup-metadata reference or null.</param>
    /// <param name="idempotencyFingerprint">The canonical idempotency fingerprint.</param>
    /// <returns>An accepted <see cref="ProjectCommandValidationResult"/>.</returns>
    public static ProjectCommandValidationResult Accepted(
        string canonicalName,
        string? canonicalDescription,
        string? canonicalSetupMetadata,
        string idempotencyFingerprint)
        => new(true, ProjectResultCode.Created, canonicalName, canonicalDescription, canonicalSetupMetadata, null, idempotencyFingerprint, null);

    /// <summary>Creates an accepted validation result for a setup update.</summary>
    /// <param name="canonicalSetup">The canonical setup.</param>
    /// <param name="idempotencyFingerprint">The canonical idempotency fingerprint.</param>
    /// <returns>An accepted <see cref="ProjectCommandValidationResult"/>.</returns>
    public static ProjectCommandValidationResult AcceptedSetup(
        ProjectSetup canonicalSetup,
        string idempotencyFingerprint)
        => new(true, ProjectResultCode.SetupUpdated, null, null, null, canonicalSetup, idempotencyFingerprint, null);

    /// <summary>Creates an accepted validation result for an archive command.</summary>
    /// <param name="idempotencyFingerprint">The canonical idempotency fingerprint.</param>
    /// <returns>An accepted <see cref="ProjectCommandValidationResult"/>.</returns>
    public static ProjectCommandValidationResult AcceptedArchive(string idempotencyFingerprint)
        => new(true, ProjectResultCode.Archived, null, null, null, null, idempotencyFingerprint, null);

    /// <summary>Creates an accepted validation result for a set-folder command.</summary>
    /// <param name="idempotencyFingerprint">The canonical idempotency fingerprint.</param>
    /// <returns>An accepted <see cref="ProjectCommandValidationResult"/>.</returns>
    public static ProjectCommandValidationResult AcceptedFolder(string idempotencyFingerprint)
        => new(true, ProjectResultCode.FolderSet, null, null, null, null, idempotencyFingerprint, null);

    /// <summary>Creates an accepted validation result for a link-file-reference command.</summary>
    /// <param name="idempotencyFingerprint">The canonical idempotency fingerprint.</param>
    /// <returns>An accepted <see cref="ProjectCommandValidationResult"/>.</returns>
    public static ProjectCommandValidationResult AcceptedFileLink(string idempotencyFingerprint)
        => new(true, ProjectResultCode.FileReferenceLinked, null, null, null, null, idempotencyFingerprint, null);

    /// <summary>Creates an accepted validation result for an unlink-file-reference command.</summary>
    /// <param name="idempotencyFingerprint">The canonical idempotency fingerprint.</param>
    /// <returns>An accepted <see cref="ProjectCommandValidationResult"/>.</returns>
    public static ProjectCommandValidationResult AcceptedFileUnlink(string idempotencyFingerprint)
        => new(true, ProjectResultCode.FileReferenceUnlinked, null, null, null, null, idempotencyFingerprint, null);

    /// <summary>Creates an accepted validation result for a link-memory command.</summary>
    /// <param name="idempotencyFingerprint">The canonical idempotency fingerprint.</param>
    /// <returns>An accepted <see cref="ProjectCommandValidationResult"/>.</returns>
    public static ProjectCommandValidationResult AcceptedMemoryLink(string idempotencyFingerprint)
        => new(true, ProjectResultCode.MemoryLinked, null, null, null, null, idempotencyFingerprint, null);

    /// <summary>Creates an accepted validation result for an unlink-memory command.</summary>
    /// <param name="idempotencyFingerprint">The canonical idempotency fingerprint.</param>
    /// <returns>An accepted <see cref="ProjectCommandValidationResult"/>.</returns>
    public static ProjectCommandValidationResult AcceptedMemoryUnlink(string idempotencyFingerprint)
        => new(true, ProjectResultCode.MemoryUnlinked, null, null, null, null, idempotencyFingerprint, null);

    /// <summary>Creates an accepted validation result for a confirm-resolution command.</summary>
    /// <param name="idempotencyFingerprint">The canonical idempotency fingerprint.</param>
    /// <returns>An accepted <see cref="ProjectCommandValidationResult"/>.</returns>
    public static ProjectCommandValidationResult AcceptedResolutionConfirmation(string idempotencyFingerprint)
        => new(true, ProjectResultCode.ProjectResolutionConfirmed, null, null, null, null, idempotencyFingerprint, null);

    /// <summary>Creates a rejected validation result carrying the code and the offending field NAME only.</summary>
    /// <param name="code">The control-flow result code.</param>
    /// <param name="rejectedField">The NAME of the rejected field (never its value), or null.</param>
    /// <returns>A rejected <see cref="ProjectCommandValidationResult"/>.</returns>
    public static ProjectCommandValidationResult Rejected(ProjectResultCode code, string? rejectedField)
        => new(false, code, null, null, null, null, null, rejectedField);
}
