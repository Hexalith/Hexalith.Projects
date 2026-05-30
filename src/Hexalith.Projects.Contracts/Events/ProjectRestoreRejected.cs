// <copyright file="ProjectRestoreRejected.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Events;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Rejection event emitted when a project-restore command is refused.
/// </summary>
/// <param name="ProjectId">The project whose restore was rejected.</param>
/// <param name="TenantId">The managed tenant that owns the project.</param>
/// <param name="Reason">The canonical rejection reason code from the shared reference-state vocabulary.</param>
/// <param name="RejectedField">Optional name of the field that failed validation (the name only, never its value).</param>
/// <param name="CorrelationId">Optional correlation identifier linking the rejection to the originating command.</param>
public sealed record ProjectRestoreRejected(
    ProjectId ProjectId,
    string TenantId,
    ReferenceState Reason,
    string? RejectedField = null,
    string? CorrelationId = null) : IRejectionEvent;
