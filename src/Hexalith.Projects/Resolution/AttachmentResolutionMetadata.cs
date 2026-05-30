// <copyright file="AttachmentResolutionMetadata.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Resolution;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Projects-owned, metadata-only view of the folder/file references presented for attachment-based
/// Project resolution (Story 4.3).
/// </summary>
/// <param name="FolderReferences">Presented Project Folder identifiers and their fail-closed state.</param>
/// <param name="FileReferences">Presented File Reference identifiers and their fail-closed state.</param>
public sealed record AttachmentResolutionMetadata(
    IReadOnlyList<AttachmentResolutionReference> FolderReferences,
    IReadOnlyList<AttachmentResolutionReference> FileReferences)
{
    /// <summary>Gets presented Project Folder identifiers.</summary>
    public IReadOnlyList<AttachmentResolutionReference> FolderReferences { get; } = FolderReferences ?? Array.Empty<AttachmentResolutionReference>();

    /// <summary>Gets presented File Reference identifiers.</summary>
    public IReadOnlyList<AttachmentResolutionReference> FileReferences { get; } = FileReferences ?? Array.Empty<AttachmentResolutionReference>();

    /// <summary>Creates a fail-closed attachment reference.</summary>
    /// <param name="referenceKind">The reference kind (<c>folder</c> or <c>file</c>).</param>
    /// <param name="referenceId">The opaque reference identifier.</param>
    /// <param name="referenceState">The fail-closed state.</param>
    /// <returns>A metadata-only attachment reference.</returns>
    public static AttachmentResolutionReference FailClosed(
        string referenceKind,
        string referenceId,
        ReferenceState referenceState = ReferenceState.Unavailable)
        => new(referenceKind, referenceId, referenceState);
}

/// <summary>One metadata-only attachment reference presented to the resolution query.</summary>
/// <param name="ReferenceKind">The reference kind (<c>folder</c> or <c>file</c>).</param>
/// <param name="ReferenceId">The opaque reference identifier.</param>
/// <param name="ReferenceState">The reference trust state.</param>
public sealed record AttachmentResolutionReference(
    string ReferenceKind,
    string ReferenceId,
    ReferenceState ReferenceState)
{
    /// <summary>Gets the reference kind.</summary>
    public string ReferenceKind { get; } = ValidateRequired(ReferenceKind, nameof(ReferenceKind));

    /// <summary>Gets the opaque reference identifier.</summary>
    public string ReferenceId { get; } = ValidateRequired(ReferenceId, nameof(ReferenceId));

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}
