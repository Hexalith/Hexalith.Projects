// <copyright file="CreateProject.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Commands;

using Hexalith.Projects.Contracts.Identifiers;

/// <summary>
/// Command to create a tenant-scoped Project workspace record (FR-1, AR-5). Imperative name, no
/// <c>Command</c> suffix, mirroring the Folders <c>CreateFolder</c> shape.
/// </summary>
/// <remarks>
/// The only required user input is the project <paramref name="Name"/>; the default lifecycle is
/// <see cref="Ui.ProjectLifecycle.Active"/>. Optional <paramref name="Description"/> and
/// <paramref name="SetupMetadata"/> carry safe, reference-only setup hints — they are boundary-
/// validated by <c>ProjectCommandValidator</c> (FR-19): raw secrets, unrestricted/local file paths,
/// unsupported reference types, and foreign-context payloads are rejected with a field NAME only,
/// never an echoed value. No Project Folder is required; auto-folder-create is deferred (do NOT call
/// the Folders client from the aggregate — a named anti-pattern). Tenant is the envelope tenant
/// (authenticated authority, never payload-controlled); identity is the validated
/// <see cref="ProjectId"/> value object.
/// </remarks>
/// <param name="TenantId">The managed tenant the project belongs to (envelope tenant, authenticated authority).</param>
/// <param name="ProjectId">The opaque, validated project identifier.</param>
/// <param name="Name">The required, non-empty project name (the only required user input).</param>
/// <param name="Description">Optional safe, metadata-only project description.</param>
/// <param name="SetupMetadata">Optional safe, reference-only setup metadata hints (field-name-validated, never echoed).</param>
/// <param name="ActorPrincipalId">The authenticated actor principal identifier.</param>
/// <param name="CorrelationId">The correlation identifier for request tracing.</param>
/// <param name="TaskId">The task identifier for task-scoped operations.</param>
/// <param name="IdempotencyKey">The idempotency key (same key + same payload = replay; same key + different payload = conflict).</param>
public sealed record CreateProject(
    string TenantId,
    ProjectId ProjectId,
    string Name,
    string? Description,
    string? SetupMetadata,
    string ActorPrincipalId,
    string CorrelationId,
    string TaskId,
    string IdempotencyKey) : IProjectCommand
{
    /// <summary>Gets the command type discriminator.</summary>
    public string CommandType => nameof(CreateProject);
}
