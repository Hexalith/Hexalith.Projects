// <copyright file="ProjectSetupUpdateRejected.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Events;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Rejection event emitted when a project setup-update command is refused (AR-6, FS-4).
/// </summary>
/// <remarks>
/// <b>Sensitivity class: metadata-only.</b> Carries no setup body, secret, token, folder path, or
/// echoed sensitive value — only safe identifiers, the canonical <see cref="ReferenceState"/> reason
/// code from the shared vocabulary, an optional safe field NAME (never its value), and a correlation
/// ID. Boundary is enforced against <c>docs/payload-taxonomy.md</c> by the FS-2 <c>NoPayloadLeakage</c>
/// harness (Story 1.4). The matching success event and command land with the update-setup command
/// story (1.8), which emits this rejection via <c>Handle</c>.
/// </remarks>
/// <param name="ProjectId">The project whose setup-update was rejected.</param>
/// <param name="TenantId">The managed tenant that owns the project.</param>
/// <param name="Reason">The canonical rejection reason code from the shared reference-state vocabulary.</param>
/// <param name="RejectedField">Optional name of the field that failed validation (the name only, never its value).</param>
/// <param name="CorrelationId">Optional correlation identifier linking the rejection to the originating command.</param>
public sealed record ProjectSetupUpdateRejected(
    ProjectId ProjectId,
    string TenantId,
    ReferenceState Reason,
    string? RejectedField = null,
    string? CorrelationId = null) : IRejectionEvent;
