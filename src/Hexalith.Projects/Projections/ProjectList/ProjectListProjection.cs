// <copyright file="ProjectListProjection.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.ProjectList;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;

/// <summary>
/// Tenant-scoped, deterministic, rebuildable read-model projection of the active project list (AR-8,
/// FS-8). Mirrors the Folders <c>FolderListProjection</c>: a private constructor over a
/// <see cref="FrozenDictionary{TKey, TValue}"/>, an <see cref="Empty"/> singleton, a pure
/// <see cref="Apply"/> with deterministic ordering (sequence, then idempotency key, then fingerprint),
/// an envelope/event tenant-agreement guard (a foreign event is skipped — never lands in another
/// tenant's bucket), and a throw-on-unknown-event policy kept in sync with <see cref="Hexalith.Projects.Aggregates.Project.ProjectStateApply"/>.
/// </summary>
/// <remarks>
/// Keys are the canonical <c>{tenant}:projects:{projectId}</c> identity derived via
/// <see cref="ProjectIdentity"/> (never a raw literal, never a payload/header/query value). The
/// rebuild/replay-determinism + duplicate-delivery idempotency proof suite is Story 1.5; here
/// <see cref="Apply"/> is simply deterministic and idempotent-tolerant so 1.5 can prove it cheaply.
/// </remarks>
public sealed record ProjectListProjection
{
    private ProjectListProjection(IReadOnlyDictionary<string, ProjectListItem> projects)
    {
        Projects = projects;
    }

    /// <summary>Gets the projected projects keyed by canonical identity.</summary>
    public IReadOnlyDictionary<string, ProjectListItem> Projects { get; }

    /// <summary>Gets the empty starting projection.</summary>
    public static ProjectListProjection Empty { get; } = new(FrozenDictionary<string, ProjectListItem>.Empty);

    /// <summary>
    /// Rebuilds the projection from a full event stream (FS-6, AR-8, NFR-7) — the explicit, repeatable
    /// "rebuild from the full event stream" entry point proven equivalent to incremental application in
    /// Story 1.5.
    /// </summary>
    /// <remarks>
    /// Defined as <b>exactly</b> <c><see cref="Empty"/>.<see cref="Apply"/>(envelopes)</c> so rebuild and
    /// incremental application share the single deterministic fold (zero duplication → they cannot drift).
    /// Pure, deterministic, order-stable (the <c>(Sequence, IdempotencyKey, IdempotencyFingerprint)</c>
    /// ordering makes the fold insensitive to source enumerable order), tenant-guarded and
    /// throw-on-unknown-event (both inherited from <see cref="Apply"/>), and uses no wall-clock / random /
    /// GUID — only event-carried data. This is the <b>in-memory</b> rebuild proof; the durable/production
    /// rebuild path (state-store reload + dead-letter replay runbook) is Story 1.9.
    /// </remarks>
    /// <param name="envelopes">The full event stream to rebuild from.</param>
    /// <returns>A projection rebuilt from the full stream, value-equal to incremental application.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an envelope carries an unknown event type.</exception>
    public static ProjectListProjection Rebuild(IEnumerable<ProjectProjectionEnvelope> envelopes)
        => Empty.Apply(envelopes);

    /// <summary>
    /// Folds a batch of projection envelopes into a new projection. Pure and deterministic.
    /// </summary>
    /// <param name="envelopes">The envelopes to apply.</param>
    /// <returns>A new projection reflecting the applied envelopes.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an envelope carries an unknown event type.</exception>
    public ProjectListProjection Apply(IEnumerable<ProjectProjectionEnvelope> envelopes)
    {
        ArgumentNullException.ThrowIfNull(envelopes);

        Dictionary<string, ProjectListItem> projects = new(Projects, StringComparer.Ordinal);

        IEnumerable<ProjectProjectionEnvelope> ordered = envelopes
            .Where(static envelope => envelope is not null)
            .Where(static envelope => envelope.Event is not null)
            // Secondary keys make replay deterministic when two envelopes share a Sequence: idempotency
            // key first (durable per-event identity), then fingerprint (durable per-content identity).
            .OrderBy(static envelope => envelope.Sequence)
            .ThenBy(static envelope => envelope.Event.IdempotencyKey, StringComparer.Ordinal)
            .ThenBy(static envelope => envelope.Event.IdempotencyFingerprint, StringComparer.Ordinal);

        foreach (ProjectProjectionEnvelope envelope in ordered)
        {
            // Envelope and event tenants must agree. A misrouted/foreign-tenant event is skipped so it
            // can never land in a different tenant's list bucket (mirrors ProjectStateApply's guard).
            if (!string.Equals(envelope.TenantId, envelope.Event.TenantId, StringComparison.Ordinal))
            {
                continue;
            }

            string? key = TryKey(envelope.Event.TenantId, envelope.Event.ProjectId);
            if (key is null)
            {
                continue;
            }

            switch (envelope.Event)
            {
                case ProjectCreated created:
                    projects[key] = new ProjectListItem(
                        created.TenantId,
                        created.ProjectId,
                        created.Name,
                        created.Lifecycle,
                        envelope.Sequence,
                        created.OccurredAt,
                        created.OccurredAt);
                    break;

                case ProjectSetupUpdated updated:
                    if (projects.TryGetValue(key, out ProjectListItem? setupRow))
                    {
                        projects[key] = setupRow with
                        {
                            Sequence = envelope.Sequence,
                            UpdatedAt = updated.OccurredAt,
                        };
                    }

                    break;

                case ProjectArchived archived:
                    if (projects.TryGetValue(key, out ProjectListItem? archivedRow))
                    {
                        projects[key] = archivedRow with
                        {
                            Lifecycle = archived.Lifecycle,
                            Sequence = envelope.Sequence,
                            UpdatedAt = archived.OccurredAt,
                        };
                    }

                    break;

                default:
                    // Diverging from ProjectStateApply (which throws on unknown event types) would let
                    // new event types replay as no-ops in the projection while the aggregate fails
                    // loudly. Throw to keep the two in sync.
                    throw new InvalidOperationException(
                        $"ProjectListProjection received an unsupported event type "
                        + $"'{envelope.Event.GetType().FullName}' at sequence {envelope.Sequence}.");
            }
        }

        return new ProjectListProjection(projects.ToFrozenDictionary(StringComparer.Ordinal));
    }

    /// <summary>Determines whether the projection contains a project for the given tenant/project.</summary>
    /// <param name="tenantId">The managed tenant identifier.</param>
    /// <param name="projectId">The project identifier value.</param>
    /// <returns><see langword="true"/> when present; otherwise <see langword="false"/>.</returns>
    public bool Contains(string tenantId, string projectId)
    {
        string? key = TryKey(tenantId, projectId);
        return key is not null && Projects.ContainsKey(key);
    }

    /// <summary>Gets the projected item for the given tenant/project, or null when absent.</summary>
    /// <param name="tenantId">The managed tenant identifier.</param>
    /// <param name="projectId">The project identifier value.</param>
    /// <returns>The item, or null.</returns>
    public ProjectListItem? Get(string tenantId, string projectId)
    {
        string? key = TryKey(tenantId, projectId);
        return key is not null && Projects.TryGetValue(key, out ProjectListItem? item) ? item : null;
    }

    /// <summary>Lists projected items for a tenant, optionally filtered by lifecycle state.</summary>
    /// <param name="tenantId">The authoritative tenant identifier.</param>
    /// <param name="lifecycleFilter">The lifecycle filter, or null for all lifecycle states.</param>
    /// <returns>The matching items in deterministic project-id order.</returns>
    public IReadOnlyList<ProjectListItem> List(string tenantId, Contracts.Ui.ProjectLifecycle? lifecycleFilter)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return [];
        }

        string tenant = tenantId.Trim();
        return Projects.Values
            .Where(item => string.Equals(item.TenantId, tenant, StringComparison.Ordinal))
            .Where(item => lifecycleFilter is null || item.Lifecycle == lifecycleFilter.Value)
            .OrderBy(item => item.ProjectId, StringComparer.Ordinal)
            .ToArray();
    }

    // Derives the canonical key via ProjectIdentity. A structurally-invalid tenant/project yields null
    // (the event is skipped) rather than a construction exception leaking from a projection fold.
    private static string? TryKey(string tenantId, string projectId)
    {
        try
        {
            return new ProjectIdentity(tenantId, new ProjectId(projectId)).GlobalId;
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentNullException)
        {
            return null;
        }
    }
}
