// <copyright file="ProjectReferenceIndexCandidateRow.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectReferenceIndex;

/// <summary>
/// Tenant-scoped, metadata-only Project row returned by the reverse reference-index read model for
/// attachment-based resolution.
/// </summary>
/// <param name="TenantId">The managed tenant identifier.</param>
/// <param name="ProjectId">The project identifier.</param>
/// <param name="DisplayName">The safe project display name.</param>
/// <param name="Lifecycle">The project lifecycle.</param>
/// <param name="MatchedReferences">The matched folder/file reference-index rows.</param>
public sealed record ProjectReferenceIndexCandidateRow(
    string TenantId,
    string ProjectId,
    string? DisplayName,
    ProjectLifecycle Lifecycle,
    IReadOnlyList<ProjectReferenceIndexItem> MatchedReferences);
