// <copyright file="ProjectsMcpParityTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Mcp.Tests;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Mcp;

using Shouldly;

using Xunit;

/// <summary>
/// Cross-surface parity: MCP descriptor vocabulary must derive from the shared Story 5.9 constants
/// and the canonical FrontComposer lifecycle set, never from adapter-local magic strings (AC8).
/// </summary>
public sealed class ProjectsMcpParityTests
{
    [Fact]
    public void Maintenance_Action_Names_Match_Shared_Vocabulary_Not_Adapter_Local_Strings()
        => ProjectsMcpDescriptors.MaintenanceActionNames.ShouldBe(
            [
                ProjectMaintenanceActions.Archive,
                ProjectMaintenanceActions.Restore,
                ProjectMaintenanceActions.Relink,
                ProjectMaintenanceActions.Unlink,
                ProjectMaintenanceActions.Reevaluate,
            ],
            ignoreOrder: false);

    [Fact]
    public void Maintenance_Descriptor_Lifecycle_Strings_Derive_From_Canonical_And_Shared_Constants()
    {
        ProjectsMcpMaintenanceActionItem item = ProjectsMcpResourceReader.ReadMaintenanceActions()[0];

        item.LifecycleWireStates.ShouldBe(string.Join(",", McpLifecycleStateNames.Canonical));
        item.WebLifecycleLabels.ShouldBe(string.Join(
            ",",
            ProjectMaintenanceCommandLifecycleStates.Idle,
            ProjectMaintenanceCommandLifecycleStates.Submitting,
            ProjectMaintenanceCommandLifecycleStates.Acknowledged,
            ProjectMaintenanceCommandLifecycleStates.Syncing,
            ProjectMaintenanceCommandLifecycleStates.Confirmed,
            ProjectMaintenanceCommandLifecycleStates.Rejected));

        // 202 acceptance must stay distinct from final confirmation (AC3).
        item.WebLifecycleLabels.ShouldContain("Acknowledged(202)");
    }

    [Fact]
    public void Reevaluate_Is_ReadOnly_While_State_Changing_Actions_Require_Confirmation_And_Idempotency()
    {
        IReadOnlyList<ProjectsMcpMaintenanceActionItem> items = ProjectsMcpResourceReader.ReadMaintenanceActions();

        ProjectsMcpMaintenanceActionItem reevaluate = items.Single(i => i.Action == ProjectMaintenanceActions.Reevaluate);
        reevaluate.RequiresConfirmation.ShouldBeFalse();
        reevaluate.RequiresIdempotencyKey.ShouldBeFalse();

        ProjectsMcpMaintenanceActionItem archive = items.Single(i => i.Action == ProjectMaintenanceActions.Archive);
        archive.RequiresConfirmation.ShouldBeTrue();
        archive.RequiresIdempotencyKey.ShouldBeTrue();
    }
}
