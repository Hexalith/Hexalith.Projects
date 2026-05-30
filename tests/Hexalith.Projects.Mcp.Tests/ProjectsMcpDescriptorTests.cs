// <copyright file="ProjectsMcpDescriptorTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Mcp.Tests;

using Hexalith.Projects.Mcp;

using Shouldly;

using Xunit;

public sealed class ProjectsMcpDescriptorTests
{
    [Fact]
    public void Manifest_Separates_ReadResources_From_MutatingTools()
    {
        ProjectsMcpDescriptors.Manifest.Resources.Select(static r => r.Name).ShouldBe([
            "projects.inventory",
            "projects.detail",
            "projects.operatorDiagnostic",
            "projects.referenceHealth",
            "projects.resolutionTrace",
            "projects.auditTimeline",
            "projects.safeDiagnosticExport",
            "projects.warningQueue",
            "projects.operationalDashboard",
            "projects.maintenanceAction",
        ], ignoreOrder: false);

        ProjectsMcpDescriptors.Manifest.Commands.Select(static c => c.ProtocolName).ShouldBe([
            "projects.archive",
            "projects.restore",
            "projects.relink",
            "projects.unlink",
            "projects.reevaluate",
        ], ignoreOrder: false);
    }

    [Fact]
    public void MutatingToolDescriptors_Require_ConfirmationEvidence_And_IdempotencyKey()
    {
        foreach (var command in ProjectsMcpDescriptors.Manifest.Commands)
        {
            command.Parameters.Select(static p => p.Name).ShouldContain(nameof(ProjectsMcpMaintenanceCommand.ProjectId));
            command.Parameters.Select(static p => p.Name).ShouldContain(nameof(ProjectsMcpMaintenanceCommand.Confirmed));
            command.Parameters.Select(static p => p.Name).ShouldContain(nameof(ProjectsMcpMaintenanceCommand.DryRunEvidence));
            command.Parameters.Select(static p => p.Name).ShouldContain(nameof(ProjectsMcpMaintenanceCommand.IdempotencyKey));
            command.Parameters.Single(p => p.Name == nameof(ProjectsMcpMaintenanceCommand.IdempotencyKey)).IsRequired.ShouldBeTrue();
        }
    }
}
