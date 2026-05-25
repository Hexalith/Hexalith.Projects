// <copyright file="ProjectDetailProjection.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.ProjectDetail;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Projections.ProjectList;

/// <summary>
/// Tenant-scoped, deterministic, rebuildable read-model projection backing the minimal
/// <c>GetProject</c> lifecycle/detail read (AR-8, FS-8). Same pattern as
/// <see cref="ProjectListProjection"/>: private constructor over a
/// <see cref="FrozenDictionary{TKey, TValue}"/>, an <see cref="Empty"/> singleton, deterministic
/// ordering, an envelope/event tenant-agreement guard, and a throw-on-unknown-event policy kept in sync
/// with <see cref="Hexalith.Projects.Aggregates.Project.ProjectStateApply"/>. Keys are the canonical <c>{tenant}:projects:{projectId}</c>
/// identity derived via <see cref="ProjectIdentity"/>.
/// </summary>
public sealed record ProjectDetailProjection
{
    private ProjectDetailProjection(IReadOnlyDictionary<string, ProjectDetailItem> projects)
    {
        Projects = projects;
    }

    /// <summary>Gets the projected project details keyed by canonical identity.</summary>
    public IReadOnlyDictionary<string, ProjectDetailItem> Projects { get; }

    /// <summary>Gets the empty starting projection.</summary>
    public static ProjectDetailProjection Empty { get; } = new(FrozenDictionary<string, ProjectDetailItem>.Empty);

    /// <summary>
    /// Folds a batch of projection envelopes into a new projection. Pure and deterministic.
    /// </summary>
    /// <param name="envelopes">The envelopes to apply.</param>
    /// <returns>A new projection reflecting the applied envelopes.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an envelope carries an unknown event type.</exception>
    public ProjectDetailProjection Apply(IEnumerable<ProjectProjectionEnvelope> envelopes)
    {
        ArgumentNullException.ThrowIfNull(envelopes);

        Dictionary<string, ProjectDetailItem> projects = new(Projects, StringComparer.Ordinal);

        IEnumerable<ProjectProjectionEnvelope> ordered = envelopes
            .Where(static envelope => envelope is not null)
            .Where(static envelope => envelope.Event is not null)
            .OrderBy(static envelope => envelope.Sequence)
            .ThenBy(static envelope => envelope.Event.IdempotencyKey, StringComparer.Ordinal)
            .ThenBy(static envelope => envelope.Event.IdempotencyFingerprint, StringComparer.Ordinal);

        foreach (ProjectProjectionEnvelope envelope in ordered)
        {
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
                    projects[key] = new ProjectDetailItem(
                        created.TenantId,
                        created.ProjectId,
                        created.Name,
                        created.Description,
                        created.SetupMetadata,
                        created.Lifecycle,
                        created.OccurredAt,
                        created.OccurredAt,
                        envelope.Sequence);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"ProjectDetailProjection received an unsupported event type "
                        + $"'{envelope.Event.GetType().FullName}' at sequence {envelope.Sequence}.");
            }
        }

        return new ProjectDetailProjection(projects.ToFrozenDictionary(StringComparer.Ordinal));
    }

    /// <summary>Gets the projected detail for the given tenant/project, or null when absent.</summary>
    /// <param name="tenantId">The managed tenant identifier.</param>
    /// <param name="projectId">The project identifier value.</param>
    /// <returns>The detail item, or null.</returns>
    public ProjectDetailItem? Get(string tenantId, string projectId)
    {
        string? key = TryKey(tenantId, projectId);
        return key is not null && Projects.TryGetValue(key, out ProjectDetailItem? item) ? item : null;
    }

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
