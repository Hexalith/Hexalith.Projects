// <copyright file="ProjectIdentity.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Identifiers;

using System;

using Hexalith.EventStore.Contracts.Identity;
using Hexalith.Projects.Contracts;

/// <summary>
/// Pure, netstandard2.0-safe canonical identity-derivation helper (AR-4, FS-3) for a Project aggregate.
/// </summary>
/// <remarks>
/// <para>
/// This is the <b>only</b> place the canonical <c>{tenant}:projects:{projectId}</c> identity and its
/// downstream keys are built. Everything downstream — actors, state store, projections, pub/sub topics,
/// SignalR groups, log scopes — derives from here, so no later story reinvents the format. The domain
/// segment is always <see cref="ProjectsContractMetadata.DomainName"/> (<c>"projects"</c>), never
/// hardcoded.
/// </para>
/// <para>
/// Every derived value is a deterministic function of <c>(tenant, projectId)</c> only — never of a
/// payload field, HTTP header, query parameter, or any non-canonical input. The same inputs always
/// yield identical derived values, and distinct tenants/projects never collide (structurally disjoint
/// because the canonical components forbid colons).
/// </para>
/// <para>
/// Reuses <see cref="AggregateIdentity"/> from <c>Hexalith.EventStore.Contracts</c> for the core
/// EventStore-owned derivations (actor id, state-store keys, pub/sub topic) rather than re-implementing
/// the colon-separated format, and layers Projects-specific projection-key / SignalR-group / log-scope
/// derivations on top. No Dapr, network, or <c>EventStore.Server</c> dependency.
/// </para>
/// </remarks>
public sealed class ProjectIdentity
{
    private readonly AggregateIdentity _identity;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectIdentity"/> class from a tenant and project.
    /// </summary>
    /// <param name="tenantId">The managed tenant identifier (non-empty, non-whitespace).</param>
    /// <param name="projectId">The project identifier value object.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/> is empty/whitespace or fails canonical validation.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> or <paramref name="projectId"/> is <see langword="null"/>.</exception>
    public ProjectIdentity(string tenantId, ProjectId projectId)
    {
        if (tenantId is null)
        {
            throw new ArgumentNullException(nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant identifier cannot be empty or whitespace.", nameof(tenantId));
        }

        if (projectId is null)
        {
            throw new ArgumentNullException(nameof(projectId));
        }

        _identity = new AggregateIdentity(tenantId, ProjectsContractMetadata.DomainName, projectId.Value);
        ProjectId = projectId;
    }

    /// <summary>Gets the managed tenant identifier (canonical, lowercase).</summary>
    public string TenantId => _identity.TenantId;

    /// <summary>Gets the canonical domain segment (always <c>"projects"</c>).</summary>
    public string Domain => _identity.Domain;

    /// <summary>Gets the project identifier.</summary>
    public ProjectId ProjectId { get; }

    /// <summary>Gets the canonical global identity in the form <c>{tenant}:projects:{projectId}</c>.</summary>
    public string GlobalId => _identity.ActorId;

    /// <summary>Gets the Dapr actor identifier (canonical <c>{tenant}:projects:{projectId}</c>).</summary>
    public string ActorId => _identity.ActorId;

    /// <summary>Gets the state-store key for the project aggregate metadata.</summary>
    public string StateStoreKey => _identity.MetadataKey;

    /// <summary>Gets the event-stream state-store key prefix.</summary>
    public string EventStreamKeyPrefix => _identity.EventStreamKeyPrefix;

    /// <summary>Gets the snapshot state-store key.</summary>
    public string SnapshotKey => _identity.SnapshotKey;

    /// <summary>Gets the pub/sub topic name for project events.</summary>
    public string PubSubTopic => _identity.PubSubTopic;

    /// <summary>Gets the SignalR group name scoped to this project.</summary>
    public string SignalRGroup => $"{GlobalId}:signalr";

    /// <summary>Gets the log-scope identifier for this project.</summary>
    public string LogScope => GlobalId;

    /// <summary>
    /// Derives a named projection key for this project (e.g. an inventory or audit projection).
    /// </summary>
    /// <param name="projectionName">The projection name (non-empty, non-whitespace).</param>
    /// <returns>A deterministic projection key in the form <c>{tenant}:projects:{projectId}:projection:{projectionName}</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="projectionName"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="projectionName"/> is <see langword="null"/>.</exception>
    public string ProjectionKey(string projectionName)
    {
        if (projectionName is null)
        {
            throw new ArgumentNullException(nameof(projectionName));
        }

        if (string.IsNullOrWhiteSpace(projectionName))
        {
            throw new ArgumentException("Projection name cannot be empty or whitespace.", nameof(projectionName));
        }

        return $"{GlobalId}:projection:{projectionName}";
    }

    /// <summary>Returns the canonical global identity string.</summary>
    /// <returns>The canonical <c>{tenant}:projects:{projectId}</c> identity.</returns>
    public override string ToString() => GlobalId;
}
