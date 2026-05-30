// <copyright file="ProjectAuditExportProjectionTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Tests.Ui;

using System.ComponentModel.DataAnnotations;
using System.Reflection;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-1 tests for Story 5.7 audit timeline and safe export UI descriptors.
/// </summary>
public sealed class ProjectAuditExportProjectionTests
{
    [Theory]
    [InlineData(typeof(ProjectAuditTimelineRowProjection))]
    [InlineData(typeof(ProjectSafeDiagnosticExportProjection))]
    public void AuditExportDescriptorsCarryFrontComposerMetadata(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        type.GetCustomAttributes(typeof(ProjectionAttribute), inherit: false).ShouldHaveSingleItem();
        ProjectionRoleAttribute role = type.GetCustomAttributes(typeof(ProjectionRoleAttribute), inherit: false)
            .ShouldHaveSingleItem()
            .ShouldBeOfType<ProjectionRoleAttribute>();
        role.Role.ShouldBe(ProjectionRole.DetailRecord);

        BoundedContextAttribute context = type.GetCustomAttributes(typeof(BoundedContextAttribute), inherit: false)
            .ShouldHaveSingleItem()
            .ShouldBeOfType<BoundedContextAttribute>();
        context.Name.ShouldBe("Projects");
        context.DisplayLabel.ShouldBe("Projects");
        type.GetCustomAttributes(typeof(DisplayAttribute), inherit: false).ShouldHaveSingleItem();
    }

    [Fact]
    public void AuditTimelineRowMapsOnlyOperatorDiagnosticFields()
    {
        ProjectOperatorAuditTimelineItem item = new(
            "audit-001",
            "project.resolution_confirmed",
            DateTimeOffset.UnixEpoch,
            "actor-001",
            "corr-001",
            "task-001",
            "conversation",
            "conversation-001",
            "NoMatch",
            "Confirmed",
            "confirmation_accepted",
            "conversation-001",
            "project-source-001",
            42);

        ProjectAuditTimelineRowProjection row = ProjectAuditTimelineRowProjection.FromAuditItem("project-001", item);

        row.ContractVersion.ShouldBe(ProjectAuditTimelineRowProjection.ContractVersionValue);
        row.ProjectId.ShouldBe("project-001");
        row.AuditEventId.ShouldBe("audit-001");
        row.OperationType.ShouldBe("project.resolution_confirmed");
        row.OccurredAt.ShouldBe(DateTimeOffset.UnixEpoch);
        row.ActorPrincipalId.ShouldBe("actor-001");
        row.CorrelationId.ShouldBe("corr-001");
        row.TaskId.ShouldBe("task-001");
        row.ReferenceKind.ShouldBe("conversation");
        row.ReferenceId.ShouldBe("conversation-001");
        row.PreviousState.ShouldBe("NoMatch");
        row.NewState.ShouldBe("Confirmed");
        row.ReasonCode.ShouldBe("confirmation_accepted");
        row.ConversationId.ShouldBe("conversation-001");
        row.SourceProjectId.ShouldBe("project-source-001");
        row.ProjectionSequence.ShouldBe(42);
    }

    [Fact]
    public void SafeExportDescriptorDeclaresStableVersionAndNoForbiddenFields()
    {
        ProjectSafeDiagnosticExportProjection.ContractVersionValue.ShouldBe("projects.safe-diagnostic-export.v1");
        ProjectSafeDiagnosticExportProjection.PayloadExclusionGuaranteeText.ShouldContain("Payload-bearing data is excluded");

        string[] forbiddenFragments =
        [
            "TenantId",
            "Transcript",
            "Prompt",
            "FilePath",
            "FileContent",
            "Workspace",
            "Secret",
            "Token",
            "CommandBody",
            "ProposalBody",
            "Idempotency",
            "CandidateScore",
            "CandidateRank",
            "RejectedCandidate",
            "SiblingDenial",
        ];

        foreach (PropertyInfo property in typeof(ProjectSafeDiagnosticExportProjection).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            forbiddenFragments.Any(fragment => property.Name.Contains(fragment, StringComparison.Ordinal))
                .ShouldBeFalse($"{property.Name} must stay metadata-only and export-safe.");
        }
    }
}
