// <copyright file="AttachmentResolutionProjectCandidate.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Resolution;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// One tenant-authorized candidate Project with folder/file references matched by the reverse
/// reference index for Story 4.3.
/// </summary>
/// <param name="ProjectId">The opaque project identifier.</param>
/// <param name="DisplayName">The optional safe project display name.</param>
/// <param name="Lifecycle">The candidate Project lifecycle.</param>
/// <param name="FolderReferences">Matched Project Folder references.</param>
/// <param name="FileReferences">Matched File Reference references.</param>
public sealed record AttachmentResolutionProjectCandidate(
    string ProjectId,
    string? DisplayName,
    ProjectLifecycle Lifecycle,
    IReadOnlyList<AttachmentResolutionReference> FolderReferences,
    IReadOnlyList<AttachmentResolutionReference> FileReferences)
{
    /// <summary>Gets the opaque project identifier.</summary>
    public string ProjectId { get; } = ValidateRequired(ProjectId, nameof(ProjectId));

    /// <summary>Gets the optional safe project display name. Null/whitespace inputs normalize to null.</summary>
    public string? DisplayName { get; } = Normalize(DisplayName);

    /// <summary>Gets matched Project Folder references.</summary>
    public IReadOnlyList<AttachmentResolutionReference> FolderReferences { get; } = FolderReferences ?? Array.Empty<AttachmentResolutionReference>();

    /// <summary>Gets matched File Reference references.</summary>
    public IReadOnlyList<AttachmentResolutionReference> FileReferences { get; } = FileReferences ?? Array.Empty<AttachmentResolutionReference>();

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
