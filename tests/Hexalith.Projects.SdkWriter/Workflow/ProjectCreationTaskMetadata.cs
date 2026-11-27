// <copyright file="ProjectCreationTaskMetadata.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Workflow;

using System;

using Hexalith.Projects.SdkWriter.Admission;

/// <summary>
/// Shared-store metadata record binding an admitted Durable Task to its reserved hidden
/// <see cref="ProjectId"/> and canonical normalized creation inputs so any replica can resume.
/// </summary>
/// <param name="TaskId">The platform Durable Task identifier.</param>
/// <param name="TenantId">The owning Tenant identifier.</param>
/// <param name="ActorPrincipalId">The original actor principal identifier.</param>
/// <param name="CallerClass">The admitted caller persona class.</param>
/// <param name="ProjectId">The reserved hidden Project identifier.</param>
/// <param name="CanonicalName">The canonical trimmed Project name.</param>
/// <param name="CanonicalDescription">The canonical trimmed optional description.</param>
/// <param name="CanonicalSetupMetadata">The canonical trimmed optional setup metadata.</param>
/// <param name="SuppliedFolderId">The optional actor-supplied Folder identifier.</param>
/// <param name="RequestSha256">The normalized lowercase SHA-256 request digest.</param>
/// <param name="CorrelationId">The correlation identifier.</param>
/// <param name="CreatedAt">The UTC timestamp of reservation.</param>
public sealed record ProjectCreationTaskMetadata(
    string TaskId,
    string TenantId,
    string ActorPrincipalId,
    ProjectCreationCallerClass CallerClass,
    string ProjectId,
    string CanonicalName,
    string? CanonicalDescription,
    string? CanonicalSetupMetadata,
    string? SuppliedFolderId,
    string RequestSha256,
    string CorrelationId,
    DateTimeOffset CreatedAt);
