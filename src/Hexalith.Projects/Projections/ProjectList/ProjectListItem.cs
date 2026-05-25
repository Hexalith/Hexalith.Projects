// <copyright file="ProjectListItem.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.ProjectList;

using System;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// A single metadata-only row in the tenant-scoped project list read model (AR-8). Mirrors the Folders
/// <c>FolderListItem</c>. Carries no transcript/file/memory body, secret, token, or path.
/// </summary>
/// <param name="TenantId">The managed tenant the project belongs to.</param>
/// <param name="ProjectId">The opaque project identifier.</param>
/// <param name="Name">The project name (metadata only).</param>
/// <param name="Lifecycle">The current lifecycle state.</param>
/// <param name="Sequence">The projection sequence watermark at which this row was last written.</param>
/// <param name="CreatedAt">The instant the project was created.</param>
/// <param name="UpdatedAt">The instant the project was last updated (equals <paramref name="CreatedAt"/> for the current create-only event set).</param>
public sealed record ProjectListItem(
    string TenantId,
    string ProjectId,
    string Name,
    ProjectLifecycle Lifecycle,
    long Sequence,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
