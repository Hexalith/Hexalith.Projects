// <copyright file="ProjectStateApply.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Aggregates.Project;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;

using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;

/// <summary>
/// Pure <c>Apply</c> for the Project aggregate (AR-3, AR-4). Mirrors the Folders <c>FolderStateApply</c>:
/// it enforces the expected canonical identity on every event (a foreign-tenant/foreign-project event
/// throws — <see cref="ProjectResultCode.TenantMismatch"/>), dedupes identical idempotent replays,
/// mutates only in-memory state, and throws on unknown event types (never a silent no-op).
/// </summary>
public static class ProjectStateApply
{
    /// <summary>
    /// Applies a single project event to the state, enforcing the expected canonical identity.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="projectEvent">The event to apply.</param>
    /// <param name="expectedIdentity">The authoritative canonical identity loaded by the caller.</param>
    /// <returns>The resulting state.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the event targets a foreign identity or is of an unknown type.</exception>
    public static ProjectState Apply(ProjectState state, IProjectEvent projectEvent, ProjectIdentity expectedIdentity)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(projectEvent);
        ArgumentNullException.ThrowIfNull(expectedIdentity);

        // Identity enforcement on the very first event too, so a misrouted event cannot poison state
        // before the IsCreated guard would fire. Derive the actual identity from the event and compare
        // against the authoritative expected identity. The exception message carries only the stable
        // result code (no event payload echo) to avoid a log-injection vector.
        string actualGlobalId = DeriveGlobalId(projectEvent);
        if (!string.Equals(actualGlobalId, expectedIdentity.GlobalId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Foreign project event in Apply: result code {ProjectResultCode.TenantMismatch}.");
        }

        // Dedupe identical replays: if the same idempotency key and fingerprint are already recorded,
        // the event has already been applied; skipping prevents silent reapplication of state mutations.
        if (!string.IsNullOrWhiteSpace(projectEvent.IdempotencyKey)
            && state.IdempotencyFingerprints.TryGetValue(projectEvent.IdempotencyKey, out string? existingFingerprint)
            && string.Equals(existingFingerprint, projectEvent.IdempotencyFingerprint, StringComparison.Ordinal))
        {
            return state;
        }

        return projectEvent switch
        {
            ProjectCreated created => state with
            {
                IsCreated = true,
                TenantId = created.TenantId,
                ProjectId = created.ProjectId,
                Name = created.Name,
                Description = created.Description,
                SetupMetadata = created.SetupMetadata,
                Lifecycle = created.Lifecycle,
                IdempotencyFingerprints = RecordIdempotency(state.IdempotencyFingerprints, projectEvent),
            },

            // Unknown event types fail loudly. Silently no-op'ing would let a future event type poison
            // the idempotency ledger on cold replay against an older code path (mirrors Folders).
            _ => throw new InvalidOperationException(
                $"Unhandled project event type: result code {ProjectResultCode.StateTransitionInvalid}."),
        };
    }

    // Derives the canonical {tenant}:projects:{projectId} identity from an event's own tenant/project
    // fields via ProjectIdentity, so the comparison uses the single derivation helper (never a raw
    // literal). A structurally-invalid event identity surfaces as a TenantMismatch (foreign) rather
    // than leaking a construction exception.
    private static string DeriveGlobalId(IProjectEvent projectEvent)
    {
        try
        {
            return new ProjectIdentity(projectEvent.TenantId, new ProjectId(projectEvent.ProjectId)).GlobalId;
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentNullException)
        {
            throw new InvalidOperationException(
                $"Malformed project event identity in Apply: result code {ProjectResultCode.TenantMismatch}.");
        }
    }

    private static IReadOnlyDictionary<string, string> RecordIdempotency(
        IReadOnlyDictionary<string, string> current,
        IProjectEvent projectEvent)
    {
        if (string.IsNullOrWhiteSpace(projectEvent.IdempotencyKey))
        {
            return current;
        }

        Dictionary<string, string> next = new(current, StringComparer.Ordinal)
        {
            [projectEvent.IdempotencyKey] = projectEvent.IdempotencyFingerprint,
        };
        return next.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
