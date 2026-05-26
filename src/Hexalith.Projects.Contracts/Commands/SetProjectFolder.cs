// <copyright file="SetProjectFolder.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Commands;

using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;

/// <summary>
/// Command to set or explicitly replace the single authorized Project Folder reference.
/// </summary>
/// <param name="TenantId">The managed tenant the project belongs to.</param>
/// <param name="ProjectId">The project identifier this command targets.</param>
/// <param name="FolderId">The Folders-owned folder identifier. Projects stores it as a sibling reference string.</param>
/// <param name="FolderMetadata">Safe folder reference metadata.</param>
/// <param name="ReplacementConfirmed">Whether replacing a different existing folder was explicitly confirmed.</param>
/// <param name="ActorPrincipalId">The authenticated actor principal identifier.</param>
/// <param name="CorrelationId">The correlation identifier for request tracing.</param>
/// <param name="TaskId">The task identifier for task-scoped operations.</param>
/// <param name="IdempotencyKey">The idempotency key.</param>
public sealed record SetProjectFolder(
    string TenantId,
    ProjectId ProjectId,
    string FolderId,
    ProjectFolderMetadata FolderMetadata,
    bool ReplacementConfirmed,
    string ActorPrincipalId,
    string CorrelationId,
    string TaskId,
    string IdempotencyKey) : IProjectCommand
{
    /// <summary>Gets the command type discriminator.</summary>
    public string CommandType => nameof(SetProjectFolder);
}
