// <copyright file="ProjectReferenceUnlinkRejected.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Events;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Rejection event emitted when a request to unlink a sibling reference (conversation, folder, file,
/// memory) from a project is refused (AR-6, FS-4).
/// </summary>
/// <remarks>
/// <b>Sensitivity class: metadata-only.</b> Carries no transcript text, file content, memory body,
/// secret, token, or echoed sensitive value — only safe identifiers (project, tenant, opaque sibling
/// reference id), the reference kind/owner-context as metadata, the canonical
/// <see cref="ReferenceState"/> reason code from the shared vocabulary, an optional safe field NAME,
/// and a correlation ID. The sibling reference is held as a plain <see cref="string"/> (ULID) per the
/// AR-7 identifier-boundary decision (<c>docs/adr/identifier-boundary.md</c>). Boundary is enforced
/// against <c>docs/payload-taxonomy.md</c> by the FS-2 <c>NoPayloadLeakage</c> harness (Story 1.4). The
/// matching success event and command land with the link/unlink command story (1.7).
/// </remarks>
/// <param name="ProjectId">The project the unlink targeted.</param>
/// <param name="TenantId">The managed tenant that owns the project.</param>
/// <param name="ReferenceKind">The reference kind/owner-context metadata (e.g. conversation, folder, file, memory).</param>
/// <param name="ReferenceId">The opaque sibling reference identifier (ULID string), reusing the owning context's representation.</param>
/// <param name="Reason">The canonical rejection reason code from the shared reference-state vocabulary.</param>
/// <param name="RejectedField">Optional name of the field that failed validation (the name only, never its value).</param>
/// <param name="CorrelationId">Optional correlation identifier linking the rejection to the originating command.</param>
public sealed record ProjectReferenceUnlinkRejected(
    ProjectId ProjectId,
    string TenantId,
    string ReferenceKind,
    string ReferenceId,
    ReferenceState Reason,
    string? RejectedField = null,
    string? CorrelationId = null) : IRejectionEvent;
