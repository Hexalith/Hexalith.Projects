// <copyright file="ProjectDetailItem.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.ProjectDetail;

using System;
using System.Collections.Generic;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// The per-project, metadata-only detail record the minimal <c>GetProject</c> read returns (AR-8).
/// Carries a freshness/sequence watermark so the read can report projection freshness. No
/// transcript/file/memory body, secret, token, or path.
/// </summary>
/// <param name="TenantId">The managed tenant the project belongs to.</param>
/// <param name="ProjectId">The opaque project identifier.</param>
/// <param name="Name">The project name (metadata only).</param>
/// <param name="Description">Optional safe, metadata-only description.</param>
/// <param name="SetupMetadata">Optional safe, reference-only setup-metadata reference.</param>
/// <param name="Setup">Optional typed setup projected additively for Story 1.8.</param>
/// <param name="ProjectFolder">The metadata-only single Project Folder reference or pending state.</param>
/// <param name="FileReferences">The bounded metadata-only optional File References, ordered by reference id.</param>
/// <param name="MemoryReferences">The bounded metadata-only optional Memory References, ordered by reference id.</param>
/// <param name="Lifecycle">The current lifecycle state.</param>
/// <param name="CreatedAt">The instant the project was created.</param>
/// <param name="UpdatedAt">The instant the project was last updated (equals <paramref name="CreatedAt"/> at creation).</param>
/// <param name="Sequence">The projection sequence watermark at which this record was last written.</param>
public sealed record ProjectDetailItem(
    string TenantId,
    string ProjectId,
    string Name,
    string? Description,
    string? SetupMetadata,
    ProjectSetup? Setup,
    ProjectFolderReference? ProjectFolder,
    IReadOnlyList<ProjectFileReference> FileReferences,
    IReadOnlyList<ProjectMemoryReference> MemoryReferences,
    ProjectLifecycle Lifecycle,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    long Sequence);
