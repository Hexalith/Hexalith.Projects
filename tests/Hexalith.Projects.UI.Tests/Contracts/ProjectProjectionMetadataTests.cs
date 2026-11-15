// <copyright file="ProjectProjectionMetadataTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Tests.Contracts;

using System;
using System.Reflection;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-1 tests for the UI-only FrontComposer projection descriptors: generator-facing metadata, shared
/// vocabulary property types, and metadata-only mapping from the existing operator DTOs. Pure: no
/// infrastructure.
/// </summary>
public sealed class ProjectProjectionMetadataTests
{
    [Fact]
    public void OperatorDiagnosticShellProjectionCarriesFrontComposerMetadata()
    {
        Type type = typeof(ProjectOperatorDiagnosticShellProjection);
        type.GetCustomAttributes(typeof(ProjectionAttribute), inherit: false).ShouldHaveSingleItem();
        BoundedContextAttribute context = type.GetCustomAttributes(typeof(BoundedContextAttribute), inherit: false)
            .ShouldHaveSingleItem()
            .ShouldBeOfType<BoundedContextAttribute>();
        context.Name.ShouldBe("Projects");
        context.DisplayLabel.ShouldBe("Projects");
        typeof(ProjectOperatorDiagnosticShellProjection)
            .GetProperty(nameof(ProjectOperatorDiagnosticShellProjection.Lifecycle))!
            .PropertyType.ShouldBe(typeof(ProjectLifecycle));
    }

    [Fact]
    public void InventoryRowProjectionCarriesFrontComposerMetadata()
    {
        Type type = typeof(ProjectInventoryRowProjection);
        type.GetCustomAttributes(typeof(ProjectionAttribute), inherit: false).ShouldHaveSingleItem();
        BoundedContextAttribute context = type.GetCustomAttributes(typeof(BoundedContextAttribute), inherit: false)
            .ShouldHaveSingleItem()
            .ShouldBeOfType<BoundedContextAttribute>();
        context.Name.ShouldBe("Projects");
        typeof(ProjectInventoryRowProjection)
            .GetProperty(nameof(ProjectInventoryRowProjection.Lifecycle))!
            .PropertyType.ShouldBe(typeof(ProjectLifecycle));
        typeof(ProjectInventoryRowProjection)
            .GetProperty(nameof(ProjectInventoryRowProjection.UpdatedAt))!
            .GetCustomAttributes(typeof(RelativeTimeAttribute), inherit: false)
            .ShouldHaveSingleItem();
        typeof(ProjectInventoryRowProjection)
            .GetProperty(nameof(ProjectInventoryRowProjection.Name))!
            .GetCustomAttributes(typeof(ColumnPriorityAttribute), inherit: false)
            .ShouldHaveSingleItem();
    }

    [Fact]
    public void DetailInspectorProjectionCarriesDetailRecordMetadata()
    {
        Type type = typeof(ProjectDetailInspectorProjection);
        type.GetCustomAttributes(typeof(ProjectionAttribute), inherit: false).ShouldHaveSingleItem();
        ProjectionRoleAttribute role = type.GetCustomAttributes(typeof(ProjectionRoleAttribute), inherit: false)
            .ShouldHaveSingleItem()
            .ShouldBeOfType<ProjectionRoleAttribute>();
        role.Role.ShouldBe(ProjectionRole.DetailRecord);
        typeof(ProjectDetailInspectorProjection)
            .GetProperty(nameof(ProjectDetailInspectorProjection.FreshnessTrustState))!
            .GetCustomAttributes(typeof(ProjectionFieldGroupAttribute), inherit: false)
            .ShouldHaveSingleItem();
    }

    [Fact]
    public void ReferenceHealthRowProjectionCarriesDetailRecordMetadata()
    {
        Type type = typeof(ProjectReferenceHealthRowProjection);
        type.GetCustomAttributes(typeof(ProjectionAttribute), inherit: false).ShouldHaveSingleItem();
        ProjectionRoleAttribute role = type.GetCustomAttributes(typeof(ProjectionRoleAttribute), inherit: false)
            .ShouldHaveSingleItem()
            .ShouldBeOfType<ProjectionRoleAttribute>();
        role.Role.ShouldBe(ProjectionRole.DetailRecord);

        PropertyInfo lastChecked = type.GetProperty(nameof(ProjectReferenceHealthRowProjection.LastCheckedAt))!;
        lastChecked.GetCustomAttributes(typeof(RelativeTimeAttribute), inherit: false).ShouldHaveSingleItem();
        type.GetProperty(nameof(ProjectReferenceHealthRowProjection.FreshnessTrustState))!
            .GetCustomAttributes(typeof(ProjectionFieldGroupAttribute), inherit: false)
            .ShouldHaveSingleItem();
    }

    [Fact]
    public void WarningQueueProjectionCarriesActionQueueMetadataAndSharedVocabularyTypes()
    {
        Type type = typeof(ProjectWarningQueueItemProjection);
        type.GetCustomAttributes(typeof(ProjectionAttribute), inherit: false).ShouldHaveSingleItem();
        ProjectionRoleAttribute role = type.GetCustomAttributes(typeof(ProjectionRoleAttribute), inherit: false)
            .ShouldHaveSingleItem()
            .ShouldBeOfType<ProjectionRoleAttribute>();
        role.Role.ShouldBe(ProjectionRole.ActionQueue);
        string whenState = role.WhenState.ShouldNotBeNull();
        whenState.ShouldContain(nameof(ReferenceState.Stale));
        whenState.ShouldContain(nameof(ReferenceState.InvalidReference));
        BoundedContextAttribute context = type.GetCustomAttributes(typeof(BoundedContextAttribute), inherit: false)
            .ShouldHaveSingleItem()
            .ShouldBeOfType<BoundedContextAttribute>();
        context.Name.ShouldBe("Projects");

        typeof(ProjectWarningQueueItemProjection)
            .GetProperty(nameof(ProjectWarningQueueItemProjection.State))!
            .PropertyType.ShouldBe(typeof(ReferenceState));
        typeof(ProjectWarningQueueItemProjection)
            .GetProperty(nameof(ProjectWarningQueueItemProjection.ReasonCode))!
            .PropertyType.ShouldBe(typeof(ProjectReasonCode?));
        typeof(ProjectWarningQueueItemProjection)
            .GetProperty(nameof(ProjectWarningQueueItemProjection.LastObservedAt))!
            .GetCustomAttributes(typeof(RelativeTimeAttribute), inherit: false)
            .ShouldHaveSingleItem();
    }

    [Fact]
    public void OperationalDashboardProjectionCarriesStatusOverviewMetadata()
    {
        Type type = typeof(ProjectOperationalDashboardProjection);
        type.GetCustomAttributes(typeof(ProjectionAttribute), inherit: false).ShouldHaveSingleItem();
        ProjectionRoleAttribute role = type.GetCustomAttributes(typeof(ProjectionRoleAttribute), inherit: false)
            .ShouldHaveSingleItem()
            .ShouldBeOfType<ProjectionRoleAttribute>();
        role.Role.ShouldBe(ProjectionRole.StatusOverview);
        typeof(ProjectOperationalDashboardProjection)
            .GetProperty(nameof(ProjectOperationalDashboardProjection.TotalVisibleProjects))!
            .GetCustomAttributes(typeof(ColumnPriorityAttribute), inherit: false)
            .ShouldHaveSingleItem();
        typeof(ProjectOperationalDashboardProjection)
            .GetProperty(nameof(ProjectOperationalDashboardProjection.LastObservedWarningAt))!
            .GetCustomAttributes(typeof(RelativeTimeAttribute), inherit: false)
            .ShouldHaveSingleItem();
    }

    [Fact]
    public void MaintenanceActionProjectionCarriesActionQueueMetadataAndSharedStates()
    {
        Type type = typeof(ProjectMaintenanceActionProjection);
        type.GetCustomAttributes(typeof(ProjectionAttribute), inherit: false).ShouldHaveSingleItem();
        ProjectionRoleAttribute role = type.GetCustomAttributes(typeof(ProjectionRoleAttribute), inherit: false)
            .ShouldHaveSingleItem()
            .ShouldBeOfType<ProjectionRoleAttribute>();
        role.Role.ShouldBe(ProjectionRole.ActionQueue);
        string whenState = role.WhenState.ShouldNotBeNull();
        whenState.ShouldContain(ProjectMaintenancePanelStates.DryRunRequired);
        whenState.ShouldContain(ProjectMaintenancePanelStates.Succeeded);

        ProjectMaintenanceActionProjection.ContractVersionValue.ShouldBe("projects.maintenance-action.ui.v1");
        ProjectMaintenanceActions.Archive.ShouldBe("archive");
        ProjectMaintenanceCommandLifecycleStates.Acknowledged.ShouldBe("Acknowledged(202)");
        typeof(ProjectMaintenanceActionProjection)
            .GetProperty(nameof(ProjectMaintenanceActionProjection.State))!
            .PropertyType.ShouldBe(typeof(ProjectMaintenancePanelState));
        typeof(ProjectMaintenanceActionProjection)
            .GetProperty(nameof(ProjectMaintenanceActionProjection.ProjectId))!
            .GetCustomAttributes(typeof(ColumnPriorityAttribute), inherit: false)
            .ShouldHaveSingleItem();
    }

    [Fact]
    public void ReferenceHealthRowProjectionMapsFromExistingReferenceSummary()
    {
        ProjectReferenceHealthRowProjection row = ProjectReferenceHealthRowProjection.FromReferenceSummary(
            "project-001",
            new ProjectOperatorReferenceSummary(
                "memory",
                "stale",
                "memory-001",
                "Case memory",
                "MemoryMatched",
                Freshness()));

        row.Id.ShouldBe("project-001:memory:memory-001");
        row.ProjectId.ShouldBe("project-001");
        row.ReferenceKind.ShouldBe("memory");
        row.OwnerContext.ShouldBe("Memories");
        row.ReferenceId.ShouldBe("memory-001");
        row.DisplayLabel.ShouldBe("Case memory");
        row.InclusionState.ShouldBe(ReferenceState.Stale);
        row.HealthState.ShouldBe(ReferenceState.Stale);
        row.ReasonCode.ShouldBe(ProjectReasonCode.MemoryMatched);
        row.LastCheckedAt.ShouldBe(DateTimeOffset.UnixEpoch);
        row.FreshnessTrustState.ShouldBe(EvidenceFreshnessStateCode.Current);
        row.ProjectionWatermark.ShouldBe("watermark-001");
        row.SafeActionAvailabilityLabel.ShouldContain("Story 5.9");
    }

    [Fact]
    public void ReferenceHealthRowProjectionFreshnessDefaultsAndAssignmentsFailClosed()
    {
        var row = new ProjectReferenceHealthRowProjection();

        row.FreshnessTrustState.ShouldBe(EvidenceFreshnessStateCode.Unavailable);

        row.FreshnessTrustState = "  FrEsH  ";
        row.FreshnessTrustState.ShouldBe(EvidenceFreshnessStateCode.Current);

        row.FreshnessTrustState = "producer-specific-value";
        row.FreshnessTrustState.ShouldBe(EvidenceFreshnessStateCode.Unavailable);
    }

    [Fact]
    public void OperatorDiagnosticShellProjectionMapsFromExistingDiagnosticDto()
    {
        ProjectOperatorDiagnostic diagnostic = new(
            "project-001",
            "Console Project",
            null,
            "active",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddMinutes(1),
            null,
            null,
            new ProjectOperatorContextActivation(true, null),
            [
                new ProjectOperatorReferenceSummary("folder", "included", "folder-001", "Folder", null, Freshness()),
                new ProjectOperatorReferenceSummary("file", "unavailable", "file-001", "File", null, Freshness()),
            ],
            [],
            Freshness());

        ProjectOperatorDiagnosticShellProjection projection =
            ProjectOperatorDiagnosticShellProjection.FromDiagnostic(diagnostic, "maintenance");

        projection.Id.ShouldBe("project-001");
        projection.ProjectId.ShouldBe(diagnostic.ProjectId);
        projection.Name.ShouldBe(diagnostic.Name);
        projection.Lifecycle.ShouldBe(ProjectLifecycle.Active);
        projection.WarningCount.ShouldBe(1);
        projection.LastUpdated.ShouldBe(diagnostic.UpdatedAt);
        projection.Mode.ShouldBe("maintenance");
        projection.FreshnessTrustState.ShouldBe("trusted");
    }

    [Fact]
    public void DetailInspectorProjectionMapsFromExistingDiagnosticDto()
    {
        ProjectOperatorDiagnostic diagnostic = new(
            "project-001",
            "Console Project",
            null,
            "active",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddMinutes(1),
            null,
            null,
            new ProjectOperatorContextActivation(false, "context_disabled"),
            [
                new ProjectOperatorReferenceSummary("folder", "included", "folder-001", "Folder", null, Freshness()),
            ],
            [
                new ProjectOperatorAuditTimelineItem(
                    "audit-001",
                    "project.created",
                    DateTimeOffset.UnixEpoch,
                    "actor-001",
                    "corr-001",
                    "task-001",
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    1),
            ],
            Freshness());

        ProjectDetailInspectorProjection projection = ProjectDetailInspectorProjection.FromDiagnostic(diagnostic);

        projection.ProjectId.ShouldBe("project-001");
        projection.Name.ShouldBe("Console Project");
        projection.Lifecycle.ShouldBe(ProjectLifecycle.Active);
        projection.ContextActivationEnabled.ShouldBeFalse();
        projection.ContextBlockedReasonCode.ShouldBe("context_disabled");
        projection.ReferenceCount.ShouldBe(1);
        projection.AuditEntryCount.ShouldBe(1);
        projection.FreshnessTrustState.ShouldBe("trusted");
    }

    private static ProjectOperatorFreshnessMetadata Freshness()
        => new("eventually_consistent", DateTimeOffset.UnixEpoch, "watermark-001", false, "trusted");
}
