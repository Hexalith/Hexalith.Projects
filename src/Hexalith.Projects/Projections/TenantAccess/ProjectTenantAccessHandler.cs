// <copyright file="ProjectTenantAccessHandler.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.TenantAccess;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Projects.Authorization;

using Microsoft.Extensions.Logging;

/// <summary>Applies normalized Tenants events to the local Projects tenant-access projection.</summary>
public sealed class ProjectTenantAccessHandler(
    IProjectTenantAccessProjectionStore store,
    IUtcClock clock,
    TenantAccessOptions options,
    ILogger<ProjectTenantAccessHandler>? logger = null)
{
    /// <summary>Applies one event using bounded optimistic-concurrency retries.</summary>
    public async Task HandleAsync(ProjectTenantAccessEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (string.IsNullOrWhiteSpace(@event.TenantId))
        {
            return;
        }

        await store.UpdateAsync(@event.TenantId, projection => ApplyOnce(projection, @event),
            Math.Max(1, options.ConcurrencyRetryAttempts), cancellationToken).ConfigureAwait(false);
    }

    private bool ApplyOnce(ProjectTenantAccessProjection projection, ProjectTenantAccessEvent @event)
    {
        if (projection.SnapshotWatermark > 0
            && (@event.SequenceNumber < projection.SnapshotWatermark
                || @event.Kind != ProjectTenantAccessEventKind.TenantAccessRefreshed
                    && @event.SequenceNumber == projection.SnapshotWatermark))
        {
            return false;
        }

        if (IsMalformed(@event))
        {
            projection.MalformedEvidence = true;
            return true;
        }

        ProjectTenantEventEvidence evidence = CreateEvidence(@event);
        if (projection.ProcessedMessages.TryGetValue(@event.MessageId, out ProjectTenantEventEvidence? existing))
        {
            if (existing != evidence)
            {
                projection.ReplayConflict = true;
                return true;
            }

            return false;
        }

        if (projection.SnapshotWatermark > 0 && @event.Kind != ProjectTenantAccessEventKind.TenantAccessRefreshed)
        {
            if (@event.SequenceNumber <= projection.Watermark)
            {
                // This unseen delta lies above the complete base but behind another delivery.
                // Its effects may be a missed revocation; only a new complete snapshot repairs trust.
                projection.MalformedEvidence = true;
                return true;
            }

            if (@event.SequenceNumber - projection.Watermark > 1)
            {
                projection.MalformedEvidence = true;
            }
        }

        if (@event.SequenceNumber <= projection.Watermark)
        {
            if (@event.SequenceNumber == projection.Watermark)
            {
                projection.ReplayConflict = true;
                return true;
            }

            // A newer live event may have overtaken a recovery whose base was incomplete.
            // Never overwrite that live event or certify the incomplete evidence as recovered.
            // A fresh recovery operation, with a fresh message ID, can restore the complete base.
            if (@event.Kind == ProjectTenantAccessEventKind.TenantAccessRefreshed
                && @event.SequenceNumber > projection.SnapshotWatermark)
            {
                projection.MalformedEvidence = true;
                return true;
            }

            logger?.LogDebug(
                "Dropping out-of-order tenant event {EventKind} for tenant {TenantId}: sequence {Sequence} <= watermark {Watermark}.",
                @event.Kind, @event.TenantId, @event.SequenceNumber, projection.Watermark);
            return false;
        }

        Apply(projection, @event);
        projection.ProcessedMessages[@event.MessageId] = evidence;
        projection.Watermark = @event.SequenceNumber;
        projection.LastEventTimestamp = @event.Timestamp;
        projection.ProjectionWatermark = $"{@event.TenantId}:{@event.SequenceNumber}";
        return true;
    }

    private static void Apply(ProjectTenantAccessProjection projection, ProjectTenantAccessEvent @event)
    {
        switch (@event.Kind)
        {
            case ProjectTenantAccessEventKind.TenantAccessRefreshed:
                ProjectTenantAccessSnapshot snapshot = @event.Snapshot!;
                projection.Enabled = snapshot.Enabled;
                projection.Principals.Clear();
                foreach (ProjectTenantPrincipalEvidence principal in snapshot.Principals)
                {
                    projection.Principals.Add(principal.PrincipalId, principal);
                }

                projection.ConfigurationKeys.Clear();
                projection.RemovedConfigurationKeys.Clear();
                foreach (string key in snapshot.ConfigurationKeys)
                {
                    AddConfigurationKey(projection, key);
                }

                projection.ReplayConflict = false;
                projection.MalformedEvidence = false;
                projection.SnapshotWatermark = @event.SequenceNumber;
                // The complete snapshot supersedes older dedup evidence and bounds retained history.
                projection.ProcessedMessages.Clear();
                break;
            case ProjectTenantAccessEventKind.TenantCreated:
            case ProjectTenantAccessEventKind.TenantEnabled:
                projection.Enabled = true;
                break;
            case ProjectTenantAccessEventKind.TenantDisabled:
                projection.Enabled = false;
                break;
            case ProjectTenantAccessEventKind.UserAddedToTenant:
            case ProjectTenantAccessEventKind.UserRoleChanged:
                if (!string.IsNullOrWhiteSpace(@event.PrincipalId) && !string.IsNullOrWhiteSpace(@event.Role))
                {
                    projection.Principals[@event.PrincipalId] = new ProjectTenantPrincipalEvidence(@event.PrincipalId, @event.Role);
                }

                break;
            case ProjectTenantAccessEventKind.UserRemovedFromTenant:
                if (!string.IsNullOrWhiteSpace(@event.PrincipalId))
                {
                    _ = projection.Principals.Remove(@event.PrincipalId);
                }

                break;
            case ProjectTenantAccessEventKind.TenantConfigurationSet:
                AddConfigurationKey(projection, @event.ConfigurationKey);
                break;
            case ProjectTenantAccessEventKind.TenantConfigurationRemoved:
                RemoveConfigurationKey(projection, @event.ConfigurationKey);
                break;
            case ProjectTenantAccessEventKind.TenantUpdated:
            default:
                break;
        }
    }

    private static void AddConfigurationKey(ProjectTenantAccessProjection projection, string? key)
    {
        if (key is null || !IsProjectsConfigurationKey(key))
        {
            return;
        }

        _ = projection.ConfigurationKeys.Add(key);
        _ = projection.RemovedConfigurationKeys.Remove(key);
    }

    private static void RemoveConfigurationKey(ProjectTenantAccessProjection projection, string? key)
    {
        if (key is null || !IsProjectsConfigurationKey(key))
        {
            return;
        }

        _ = projection.ConfigurationKeys.Remove(key);
        _ = projection.RemovedConfigurationKeys.Add(key);
    }

    private bool IsMalformed(ProjectTenantAccessEvent @event)
        => string.IsNullOrWhiteSpace(@event.MessageId)
            || @event.SequenceNumber <= 0
            || @event.Timestamp - clock.UtcNow > options.ClockSkewTolerance
            || (@event.Kind == ProjectTenantAccessEventKind.TenantAccessRefreshed && !IsValidSnapshot(@event.Snapshot))
            || ((@event.Kind is ProjectTenantAccessEventKind.UserAddedToTenant
                or ProjectTenantAccessEventKind.UserRemovedFromTenant
                or ProjectTenantAccessEventKind.UserRoleChanged)
                && string.IsNullOrWhiteSpace(@event.PrincipalId));

    private static bool IsValidSnapshot(ProjectTenantAccessSnapshot? snapshot)
        => snapshot is not null && snapshot.Principals is not null && snapshot.ConfigurationKeys is not null
            && snapshot.Principals.All(principal => principal is not null
                && !string.IsNullOrWhiteSpace(principal.PrincipalId)
                && principal.Role is "TenantOwner" or "TenantContributor" or "TenantReader")
            && snapshot.Principals.Select(principal => principal.PrincipalId).Distinct(StringComparer.Ordinal).Count() == snapshot.Principals.Count
            && snapshot.ConfigurationKeys.All(key => key is not null && key.Length > 0)
            && snapshot.ConfigurationKeys.Distinct(StringComparer.Ordinal).Count() == snapshot.ConfigurationKeys.Count;

    private static bool IsProjectsConfigurationKey(string key)
        => key.StartsWith("projects.", StringComparison.Ordinal);

    private static ProjectTenantEventEvidence CreateEvidence(ProjectTenantAccessEvent @event)
        => new(
            @event.MessageId,
            @event.TenantId,
            @event.Kind.ToString(),
            @event.SequenceNumber,
            @event.Timestamp,
            @event.PayloadFingerprint);
}
